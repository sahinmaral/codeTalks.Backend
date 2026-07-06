using codeTalks.Application.Services.Notifications.Interfaces;
using StackExchange.Redis;

namespace codeTalks.Infrastructure.Notifications;


public class RedisConnectionTracker(IConnectionMultiplexer multiplexer) : IConnectionTracker
{
    private readonly IDatabase _redis = multiplexer.GetDatabase();
    private const string Prefix = "hub:connections:";

    public async Task TrackAsync(string userId, string connectionId, CancellationToken ct = default)
        => await _redis.SetAddAsync($"{Prefix}{userId}", connectionId);

    public async Task UntrackAsync(string userId, string connectionId, CancellationToken ct = default)
        => await _redis.SetRemoveAsync($"{Prefix}{userId}", connectionId);

    public async Task<bool> IsUserOnlineAsync(string userId, CancellationToken ct = default)
        => await _redis.KeyExistsAsync($"{Prefix}{userId}");

    public async Task<IEnumerable<string>> GetConnectionIdsAsync(string userId, CancellationToken ct = default)
    {
        var members = await _redis.SetMembersAsync($"{Prefix}{userId}");
        return members.Select(m => m.ToString());
    }
}