using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Domain;
using Microsoft.Extensions.Logging;

namespace codeTalks.Application.Services.Notifications;

public class NotificationDecisionEngine(ILogger<NotificationDecisionEngine> logger)
{
    public DeliveryDecision Decide(UserDeliveryContext ctx)
    {
        logger.LogInformation($"Is Notification Enabled: ${ctx.NotificationsEnabled}");
        logger.LogInformation($"Is Sound Enabled: ${ctx.IsSoundEnabled}");
        logger.LogInformation($"Is Channel Muted: ${ctx.IsChannelMuted}");
        logger.LogInformation($"Is Connected: ${ctx.IsConnected}");
        logger.LogInformation($"User Status: ${ctx.Status}");
        
        if (!ctx.NotificationsEnabled)
            return new DeliveryDecision(DeliveryMode.Drop, false);

        if (ctx.IsChannelMuted)
            return new DeliveryDecision(DeliveryMode.SignalRSilent, false);
        
        if (!ctx.IsConnected)
            return new DeliveryDecision(DeliveryMode.SignalRAndPush, false);
        
        var mode = ctx.Status switch
        {
            UserStatusType.Online    => DeliveryMode.SignalRSound,
            UserStatusType.Away      => DeliveryMode.SignalRAndPush,
            UserStatusType.Busy      => DeliveryMode.SignalRSilent,
            UserStatusType.Invisible => DeliveryMode.SignalRSound,
            _                        => DeliveryMode.SignalRSound
        };

        var withSound = ctx.IsSoundEnabled && mode == DeliveryMode.SignalRSound;

        return new DeliveryDecision(mode, withSound);
    }
}