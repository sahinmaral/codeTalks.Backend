using codeTalks.Domain;

namespace codeTalks.Application.Services.Notifications;

public record UserDeliveryContext(
    string UserId,
    bool IsConnected,
    UserStatusType Status,
    bool NotificationsEnabled,
    bool IsSoundEnabled,
    bool IsChannelMuted 
);