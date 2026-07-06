namespace codeTalks.Application.Services.Notifications.Interfaces;

public interface IConnectionTracker
{
    Task TrackAsync(string userId, string connectionId, CancellationToken ct = default);
    Task UntrackAsync(string userId, string connectionId, CancellationToken ct = default);
    Task<bool> IsUserOnlineAsync(string userId, CancellationToken ct = default);
    Task<IEnumerable<string>> GetConnectionIdsAsync(string userId, CancellationToken ct = default);
}