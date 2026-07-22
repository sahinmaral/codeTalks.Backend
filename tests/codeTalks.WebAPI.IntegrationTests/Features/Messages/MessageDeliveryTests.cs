using System.Net.Http.Json;
using codeTalks.Application.Features.Channels.Commands.JoinChannel;
using codeTalks.Application.Features.Messages.Commands.CreateMessage;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Domain;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace codeTalks.WebAPI.IntegrationTests.Features.Messages;

/// <summary>
/// Proves the real async delivery pipeline: POST /api/messages publishes a
/// <c>ChannelMessageCreatedEvent</c> to the real RabbitMQ container, the real
/// <c>ChannelMessageFanoutWorker</c> consumes it, resolves channel members, and calls
/// <c>ChannelFanoutService</c> per recipient — incrementing the real Redis-backed unread
/// tracker and invoking <see cref="IPushNotificationProvider"/> (faked; the only real
/// third-party network call in this path). Because delivery happens off the request
/// thread, assertions poll via <see cref="IntegrationTestBase.WaitUntilAsync"/> instead of
/// checking synchronously.
/// </summary>
public sealed class MessageDeliveryTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Creating_a_message_increments_the_recipients_real_unread_count()
    {
        var (_, senderClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(senderClient, ChannelJoinPolicy.Open);
        var (_, recipientClient) = await JoinAsync(channel);

        await PostMessageAsync(senderClient, channel.Id, "hello");

        await WaitUntilAsync(async () =>
            await GetIntAsync(recipientClient, $"/api/notifications/unread-count/{channel.Id}") == 1);
    }

    [Fact]
    public async Task Creating_a_message_triggers_a_push_notification_for_a_disconnected_recipient()
    {
        var (_, senderClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(senderClient, ChannelJoinPolicy.Open);
        var (recipient, _) = await JoinAsync(channel);

        var pushProvider = Factory.Services.GetRequiredService<IPushNotificationProvider>();

        await PostMessageAsync(senderClient, channel.Id, "hello");

        // Recipient never opens a SignalR connection, so NotificationDecisionEngine
        // decides SignalRAndPush; wait for the (async) push call to land.
        await WaitUntilAsync(() => Task.FromResult(
            pushProvider.ReceivedCalls().Any(c =>
                c.GetArguments()[0] as string == recipient.UserId)));

        await pushProvider.Received(1).SendPushAsync(
            recipient.UserId, Arg.Any<ChannelMessagePayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creating_a_message_does_not_affect_a_non_members_unread_count()
    {
        var (_, senderClient) = await CreateUserAsync();
        var channel = await CreateChannelAsync(senderClient, ChannelJoinPolicy.Open);
        var (_, recipientClient) = await JoinAsync(channel);
        var (_, outsiderClient) = await CreateUserAsync();
        var outsiderChannel = await CreateChannelAsync(outsiderClient);

        await PostMessageAsync(senderClient, channel.Id, "hello");

        // Give the real pipeline a chance to run, then confirm it never touched an
        // unrelated user's count for an unrelated channel.
        await WaitUntilAsync(async () =>
            await GetIntAsync(recipientClient, $"/api/notifications/unread-count/{channel.Id}") == 1);

        (await GetIntAsync(outsiderClient, $"/api/notifications/unread-count/{outsiderChannel.Id}"))
            .Should().Be(0);
    }

    // ---- helpers -------------------------------------------------------------

    private async Task<(AuthenticatedUser User, HttpClient Client)> JoinAsync(ChannelInfo channel)
    {
        var (user, client) = await CreateUserAsync();
        var join = await client.PostAsJsonAsync("/api/channels/join", new JoinChannelCommand
        {
            InviteCode = channel.InviteCode
        });
        join.EnsureSuccessStatusCode();
        return (user, client);
    }

    private static async Task PostMessageAsync(HttpClient client, string channelId, string content)
    {
        var response = await client.PostAsJsonAsync("/api/messages", new CreateMessageCommand
        {
            Content = content,
            ChannelId = channelId
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task<int> GetIntAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>(JsonWebOptions);
    }
}
