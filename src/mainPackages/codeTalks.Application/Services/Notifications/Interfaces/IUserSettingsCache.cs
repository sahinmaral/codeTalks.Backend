using codeTalks.Application.Services.Notifications.Models;

namespace codeTalks.Application.Services.Notifications.Interfaces;

public interface IUserSettingsCache
{
    Task<CachedNotificationSetting> GetNotificationSettingAsync(
        string userId, CancellationToken ct = default);

    Task SetNotificationSettingAsync(
        string userId, bool isEnabled, bool isSoundEnabled);

    Task<CachedMuteSetting?> GetChannelMuteSettingAsync(
        string userId, string channelId, CancellationToken ct = default);

    Task SetChannelMuteSettingAsync(
        string userId, string channelId, bool isMuted, DateTime? mutedUntil);

    Task InvalidateNotificationSettingAsync(string userId);
    Task InvalidateMuteSettingAsync(string userId, string channelId);
}