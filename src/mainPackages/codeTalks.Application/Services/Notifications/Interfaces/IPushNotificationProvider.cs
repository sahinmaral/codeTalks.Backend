using codeTalks.Application.Services.Notifications.Models;

namespace codeTalks.Application.Services.Notifications.Interfaces;

public interface IPushNotificationProvider
{
    Task SendPushAsync(string userId, ChannelMessagePayload payload, CancellationToken ct = default);
}