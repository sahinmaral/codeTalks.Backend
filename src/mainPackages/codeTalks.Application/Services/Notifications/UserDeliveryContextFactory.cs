using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;

namespace codeTalks.Application.Services.Notifications;

public class UserDeliveryContextFactory(
    IConnectionTracker connectionTracker,
    IUserSettingsCache settingsCache,
    IUserStatusRepository statusRepository) 
    : IUserDeliveryContextFactory
{
    public async Task<UserDeliveryContext> CreateAsync(
        string userId, string channelId, CancellationToken ct)
    {
        // Redis-backed, safe to run concurrently with the DB work below.
        var isConnectedTask = connectionTracker.IsUserOnlineAsync(userId, ct);

        // These share a single scoped DbContext (on cache miss), which is not
        // thread-safe — they must be awaited sequentially, never in parallel.
        var settings    = await settingsCache.GetNotificationSettingAsync(userId, ct);
        var muteSetting  = await settingsCache.GetChannelMuteSettingAsync(userId, channelId, ct);
        var status      = await statusRepository.GetAsync(x => x.UserId == userId, ct);

        var isConnected = await isConnectedTask;

        return new UserDeliveryContext(
            UserId: userId,
            IsConnected: isConnected,
            Status: status?.Status ?? UserStatusType.Online,
            NotificationsEnabled: settings.IsEnabled,
            IsSoundEnabled: settings.IsSoundEnabled,
            IsChannelMuted: muteSetting?.IsMuted ?? false
        );
    }
}