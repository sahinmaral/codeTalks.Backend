using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Persistence.Repositories;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace codeTalks.Infrastructure.Notifications;

public class ExpoPushNotificationProvider(IUserDeviceRepository deviceRepository, ILogger<ExpoPushNotificationProvider> logger)
    : IPushNotificationProvider
{
    private const string ExpoApiUrl = "https://exp.host/--/api/v2/push/send";

    public async Task SendPushAsync(
        string userId, ChannelMessagePayload payload, CancellationToken ct = default)
    {
        try
        {
            var devices = await deviceRepository.GetAllAsync(
                x => x.UserId == userId, ct);

            logger.LogInformation($"SendPushAsync called for userId: {userId}, device count: {devices.Count}");

            if (!devices.Any())
            {
                logger.LogWarning("No devices found for userId: {UserId}", userId);
                return;
            }

            var messages = devices.Select(d => new
            {
                to      = d.DeviceToken,
                title   = payload.ChannelName,
                body    = $"{payload.SenderName}: {payload.Content}",
                sound   = payload.WithSound ? "default" : (string?)null,
                data    = new
                {
                    channelId = payload.ChannelId,
                    messageId = payload.MessageId,
                    senderId  = payload.SenderId,
                    withSound = payload.WithSound.ToString().ToLower()
                },
                channelId = payload.WithSound ? "default" : "silent"
            }).ToList();

            logger.LogInformation($"Sending push to tokens: {string.Join(", ", devices.Select(d => d.DeviceToken))}");

            var response = await ExpoApiUrl
                .PostJsonAsync(messages, cancellationToken: ct);

            logger.LogInformation($"Expo response status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError($"SendPushAsync error: {ex}");
        }
    }
}