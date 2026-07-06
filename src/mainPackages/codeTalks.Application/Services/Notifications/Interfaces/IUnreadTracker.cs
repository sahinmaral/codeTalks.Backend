namespace codeTalks.Application.Services.Notifications.Interfaces;

public interface IUnreadTracker
{
    Task IncrementAsync(string userId, string channelId, CancellationToken ct = default);
    Task ResetAsync(string userId, string channelId, CancellationToken ct = default);
    Task<long> GetCountAsync(string userId, string channelId, CancellationToken ct = default);
}