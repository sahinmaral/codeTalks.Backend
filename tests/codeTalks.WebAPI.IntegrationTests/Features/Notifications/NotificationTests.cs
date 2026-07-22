using System.Net;
using System.Net.Http.Json;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.WebAPI.IntegrationTests.Features.Notifications;

/// <summary>
/// Full-pipeline coverage of /api/notifications (unread counts), backed by the real,
/// Redis-container-backed <see cref="IUnreadTracker"/>. Tests seed counts through the
/// tracker's own interface (<see cref="SeedAsync"/>) and assert the endpoints read them
/// back — the total sums the current user's Accepted channels, and reset clears a channel.
/// </summary>
public sealed class NotificationTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Unread_count_without_token_returns_401()
    {
        var response = await Client.GetAsync("/api/notifications/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_channel_unread_count_reflects_the_tracker()
    {
        var (user, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);
        await SeedAsync(user.UserId, channel.Id, 5);

        var count = await GetIntAsync(client, $"/api/notifications/unread-count/{channel.Id}");

        count.Should().Be(5);
    }

    [Fact]
    public async Task Get_total_unread_count_sums_accepted_channels()
    {
        var (user, client) = await CreateUserAsync();
        var channelA = await CreateChannelAsync(client);
        var channelB = await CreateChannelAsync(client);
        await SeedAsync(user.UserId, channelA.Id, 3);
        await SeedAsync(user.UserId, channelB.Id, 4);

        var total = await GetIntAsync(client, "/api/notifications/unread-count");

        total.Should().Be(7);
    }

    [Fact]
    public async Task Total_unread_count_is_zero_without_activity()
    {
        var (_, client) = await CreateUserAsync();
        await CreateChannelAsync(client);

        var total = await GetIntAsync(client, "/api/notifications/unread-count");

        total.Should().Be(0);
    }

    [Fact]
    public async Task Reset_channel_unread_count_clears_it()
    {
        var (user, client) = await CreateUserAsync();
        var channel = await CreateChannelAsync(client);
        await SeedAsync(user.UserId, channel.Id, 9);

        var reset = await client.PostAsync($"/api/notifications/reset/{channel.Id}", null);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetIntAsync(client, $"/api/notifications/unread-count/{channel.Id}")).Should().Be(0);
    }

    /// <summary>Directly drives the real tracker to a known count, bypassing the message pipeline.</summary>
    private async Task SeedAsync(string userId, string channelId, int count)
    {
        // IUnreadTracker is registered Scoped in production code; resolving it straight
        // from Factory.Services (the root provider) throws, so use a scope like the
        // handlers do.
        await using var scope = CreateScope();
        var tracker = scope.ServiceProvider.GetRequiredService<IUnreadTracker>();

        for (var i = 0; i < count; i++)
            await tracker.IncrementAsync(userId, channelId);
    }

    private async Task<int> GetIntAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>(JsonWebOptions);
    }
}