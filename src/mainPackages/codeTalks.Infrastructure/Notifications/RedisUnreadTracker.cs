using codeTalks.Application.Services.Notifications.Interfaces;
using StackExchange.Redis;

namespace codeTalks.Infrastructure.Notifications;

public class RedisUnreadTracker(IConnectionMultiplexer multiplexer) : IUnreadTracker
{
    private readonly IDatabase _redis = multiplexer.GetDatabase();
    private const string Prefix = "unread:";

    public async Task IncrementAsync(string userId, string channelId, CancellationToken ct = default)
        => await _redis.StringIncrementAsync($"{Prefix}{userId}:{channelId}");

    public async Task ResetAsync(string userId, string channelId, CancellationToken ct = default)
        => await _redis.KeyDeleteAsync($"{Prefix}{userId}:{channelId}");

    public async Task<long> GetCountAsync(string userId, string channelId, CancellationToken ct = default)
    {
        var value = await _redis.StringGetAsync($"{Prefix}{userId}:{channelId}");
        return value.HasValue ? (long)value : 0;
    }
}