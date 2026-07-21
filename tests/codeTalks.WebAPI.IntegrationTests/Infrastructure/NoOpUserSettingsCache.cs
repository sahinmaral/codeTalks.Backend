using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Domain;

namespace codeTalks.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Test double for <see cref="IUserSettingsCache"/>. The real implementation is Redis-backed;
/// integration tests run with a stubbed multiplexer, so cache reads return defaults and writes
/// are no-ops. The source of truth for these tests is Postgres, which the handlers write to
/// before touching the cache.
/// </summary>
public sealed class NoOpUserSettingsCache : IUserSettingsCache
{
    public Task<CachedNotificationSetting> GetNotificationSettingAsync(string userId, CancellationToken ct = default) =>
        Task.FromResult(new CachedNotificationSetting(true, false, UserStatusType.Online));

    public Task SetNotificationSettingAsync(string userId, bool isEnabled, bool isSoundEnabled) => Task.CompletedTask;

    public Task<CachedMuteSetting?> GetChannelMuteSettingAsync(string userId, string channelId, CancellationToken ct = default) =>
        Task.FromResult<CachedMuteSetting?>(null);

    public Task SetChannelMuteSettingAsync(string userId, string channelId, bool isMuted, DateTime? mutedUntil) => Task.CompletedTask;

    public Task InvalidateNotificationSettingAsync(string userId) => Task.CompletedTask;

    public Task InvalidateMuteSettingAsync(string userId, string channelId) => Task.CompletedTask;
}