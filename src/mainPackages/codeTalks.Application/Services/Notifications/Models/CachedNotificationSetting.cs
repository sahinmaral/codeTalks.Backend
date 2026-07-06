using codeTalks.Domain;

namespace codeTalks.Application.Services.Notifications.Models;

public record CachedNotificationSetting(
    bool IsEnabled,
    bool IsSoundEnabled,
    UserStatusType Status
);