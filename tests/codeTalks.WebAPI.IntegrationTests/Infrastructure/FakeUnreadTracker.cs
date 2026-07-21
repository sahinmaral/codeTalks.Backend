using System.Collections.Concurrent;
using codeTalks.Application.Services.Notifications.Interfaces;

namespace codeTalks.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// In-memory stand-in for the Redis-backed <see cref="IUnreadTracker"/>. Registered as a
/// singleton so a test can seed counts (via <see cref="Seed"/>) and the request handler reads
/// the same state back. Thread-safe because the WhenAll in GetUnreadCount fans out concurrently.
/// </summary>
public sealed class FakeUnreadTracker : IUnreadTracker
{
    private readonly ConcurrentDictionary<(string UserId, string ChannelId), long> _counts = new();

    /// <summary>Test helper: directly set a per-channel unread count.</summary>
    public void Seed(string userId, string channelId, long count) => _counts[(userId, channelId)] = count;

    public Task IncrementAsync(string userId, string channelId, CancellationToken ct = default)
    {
        _counts.AddOrUpdate((userId, channelId), 1, (_, current) => current + 1);
        return Task.CompletedTask;
    }

    public Task ResetAsync(string userId, string channelId, CancellationToken ct = default)
    {
        _counts.TryRemove((userId, channelId), out _);
        return Task.CompletedTask;
    }

    public Task<long> GetCountAsync(string userId, string channelId, CancellationToken ct = default) =>
        Task.FromResult(_counts.GetValueOrDefault((userId, channelId)));
}