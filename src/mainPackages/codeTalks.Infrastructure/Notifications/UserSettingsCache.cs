using System.Text.Json;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using StackExchange.Redis;

namespace codeTalks.Infrastructure.Notifications;

public class UserSettingsCache(
    IConnectionMultiplexer multiplexer,
    IUserNotificationSettingRepository notifRepo,
    IUserChannelMuteSettingRepository muteRepo)
    : IUserSettingsCache
{
    private readonly IDatabase _redis = multiplexer.GetDatabase();

    private static readonly TimeSpan NotifSettingTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MuteSettingTtl  = TimeSpan.FromMinutes(5);

    public async Task<CachedNotificationSetting> GetNotificationSettingAsync(
        string userId, CancellationToken ct = default)
    {
        var key    = $"notif-setting:{userId}";
        var cached = await _redis.StringGetAsync(key);

        if (cached.HasValue)
            return JsonSerializer.Deserialize<CachedNotificationSetting>(cached!)!;

        // Cache miss — go to DB
        var setting = await notifRepo.GetByUserIdAsync(userId, ct);

        // If user has no setting yet, fall back to safe defaults
        var result = setting is null
            ? new CachedNotificationSetting(IsEnabled: true, IsSoundEnabled: true, Status: UserStatusType.Online)
            : new CachedNotificationSetting(setting.IsEnabled, setting.IsSoundEnabled, UserStatusType.Online);

        await _redis.StringSetAsync(key, JsonSerializer.Serialize(result), NotifSettingTtl);

        return result;
    }

    public async Task<CachedMuteSetting?> GetChannelMuteSettingAsync(
        string userId, string channelId, CancellationToken ct = default)
    {
        var key    = $"mute:{userId}:{channelId}";
        var cached = await _redis.StringGetAsync(key);

        if (cached.HasValue)
            return JsonSerializer.Deserialize<CachedMuteSetting>(cached!);

        // Cache miss — go to DB
        var mute = await muteRepo.GetAsync(userId, channelId, ct);

        if (mute is null)
            return null;

        var result = new CachedMuteSetting(mute.IsMuted, mute.MutedUntil);

        await _redis.StringSetAsync(key, JsonSerializer.Serialize(result), MuteSettingTtl);

        return result;
    }

    public async Task SetNotificationSettingAsync(
        string userId, bool isEnabled, bool isSoundEnabled)
    {
        var key = $"notif-setting:{userId}";
        
        var result = new CachedNotificationSetting(isEnabled, isSoundEnabled, UserStatusType.Online);

        await _redis.StringSetAsync(key, JsonSerializer.Serialize(result), NotifSettingTtl);
    }

    public async Task SetChannelMuteSettingAsync(
        string userId, string channelId, bool isMuted, DateTime? mutedUntil)
    {
        var key    = $"mute:{userId}:{channelId}";
        var result = new CachedMuteSetting(isMuted, mutedUntil);

        await _redis.StringSetAsync(key, JsonSerializer.Serialize(result), MuteSettingTtl);
    }

    public async Task InvalidateNotificationSettingAsync(string userId)
        => await _redis.KeyDeleteAsync($"notif-setting:{userId}");

    public async Task InvalidateMuteSettingAsync(string userId, string channelId)
        => await _redis.KeyDeleteAsync($"mute:{userId}:{channelId}");
}