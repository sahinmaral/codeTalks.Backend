namespace codeTalks.Application.Services.Notifications.Models;

public record CachedMuteSetting(
    bool IsMuted,
    DateTime? MutedUntil
);