using System.Text;
using System.Text.Json;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Notifications.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace codeTalks.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string ExchangeName = "channel.messages";
    private const string QueueName = "channel-fanout-q";
    private const string RoutingKey = "channel.message";

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Value.Host,
            UserName = options.Value.Username,
            Password = options.Value.Password
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel    = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true).GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false).GetAwaiter().GetResult();

        _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey).GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(T message, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            Persistent  = true,
            ContentType = "application/json"
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}