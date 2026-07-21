using System.Net;
using System.Net.Http.Json;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.WebAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace codeTalks.WebAPI.IntegrationTests.Features.Notifications;

/// <summary>
/// Full-pipeline coverage of /api/notifications (unread counts). The Redis-backed
/// <see cref="IUnreadTracker"/> is swapped for an in-memory <see cref="FakeUnreadTracker"/>
/// singleton, so tests seed counts directly and assert the endpoints read them back — the
/// total sums the current user's Accepted channels, and reset clears a channel.
/// </summary>
public sealed class NotificationTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private FakeUnreadTracker Tracker => (FakeUnreadTracker)Factory.Services.GetRequiredService<IUnreadTracker>();

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
        Tracker.Seed(user.UserId, channel.Id, 5);

        var count = await GetIntAsync(client, $"/api/notifications/unread-count/{channel.Id}");

        count.Should().Be(5);
    }

    [Fact]
    public async Task Get_total_unread_count_sums_accepted_channels()
    {
        var (user, client) = await CreateUserAsync();
        var channelA = await CreateChannelAsync(client);
        var channelB = await CreateChannelAsync(client);
        Tracker.Seed(user.UserId, channelA.Id, 3);
        Tracker.Seed(user.UserId, channelB.Id, 4);

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
        Tracker.Seed(user.UserId, channel.Id, 9);

        var reset = await client.PostAsync($"/api/notifications/reset/{channel.Id}", null);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetIntAsync(client, $"/api/notifications/unread-count/{channel.Id}")).Should().Be(0);
    }

    private async Task<int> GetIntAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>(JsonWebOptions);
    }
}