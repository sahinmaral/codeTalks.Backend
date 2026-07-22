using System.Text;
using System.Text.Json;
using codeTalks.Application.Services.Notifications;
using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Infrastructure.Notifications;
using Core.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace codeTalks.Infrastructure.Messaging;

public class ChannelMessageFanoutWorker(
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "channel.messages";
    private const string QueueName = "channel-fanout-q";
    private const string RoutingKey = "channel.message";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Value.Host,
            Port = options.Value.Port,
            UserName = options.Value.Username,
            Password = options.Value.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            var evt  = JsonSerializer.Deserialize<ChannelMessageCreatedEvent>(json)!;

            // Get member ids in one scope
            List<string> memberIds;
            using (var scope = scopeFactory.CreateScope())
            {
                var channelRepo = scope.ServiceProvider
                    .GetRequiredService<IChannelRepository>();

                var members = await channelRepo.GetChannelUsersAsync(
                    x => x.ChannelId == evt.ChannelId &&
                         x.Status == ChannelUserStatus.Accepted &&
                         x.UserId != evt.SenderId,
                    CancellationToken.None);

                memberIds = members.Select(m => m.UserId).ToList();
            }

            // Create a separate scope per recipient — no shared DbContext
            await Parallel.ForEachAsync(memberIds,
                new ParallelOptions { MaxDegreeOfParallelism = 20 },
                async (userId, ct) =>
                {
                    using var scope = scopeFactory.CreateScope();
                    var fanoutService = scope.ServiceProvider
                        .GetRequiredService<ChannelFanoutService>();

                    await fanoutService.DeliverToUserAsync(userId, evt, ct);
                });

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FanoutWorker error: {ex}");
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}