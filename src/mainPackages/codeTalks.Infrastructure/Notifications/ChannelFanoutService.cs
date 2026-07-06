using codeTalks.Application.Services.Notifications;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Notifications.Models;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Infrastructure.Hubs;
using Core.Persistence.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace codeTalks.Infrastructure.Notifications;

public class ChannelFanoutService(
    IUserDeliveryContextFactory contextFactory,
    NotificationDecisionEngine decisionEngine,
    IHubContext<NotificationHub> hubContext,
    IPushNotificationProvider pushProvider,
    IUnreadTracker unreadTracker,
    IConnectionTracker connectionTracker,
    ILogger<ChannelFanoutService> logger)
{
    public async Task DeliverToUserAsync(
        string userId, ChannelMessageCreatedEvent evt, CancellationToken ct)
    {
        var context  = await contextFactory.CreateAsync(userId, evt.ChannelId, ct);
        var decision = decisionEngine.Decide(context);
        
        logger.LogInformation(
            "Delivering to userId: {UserId}, Mode: {Mode}, IsConnected: {IsConnected}",
            userId, decision.Mode, context.IsConnected);
        
        await ExecuteDeliveryAsync(context, decision, evt, ct);
    }

    private async Task ExecuteDeliveryAsync(
        UserDeliveryContext ctx,
        DeliveryDecision decision,
        ChannelMessageCreatedEvent evt,
        CancellationToken ct)
    {
        await unreadTracker.IncrementAsync(ctx.UserId, evt.ChannelId, ct);
        
        logger.LogInformation("ExecuteDeliveryAsync: Mode={Mode}, IsConnected={IsConnected}",
            decision.Mode, ctx.IsConnected);
        
        if (decision.Mode == DeliveryMode.Drop)
        {
            logger.LogInformation("Notification dropped for userId: {UserId}", ctx.UserId);
            return;
        }

        var payload = new ChannelMessagePayload(
            evt.MessageId,
            evt.ChannelId,
            evt.ChannelName,
            evt.SenderId,
            evt.SenderName,
            evt.Content,
            evt.SentAt,
            decision.WithSound);

        if (ctx.IsConnected)
        {
            var connectionIds = await connectionTracker
                .GetConnectionIdsAsync(ctx.UserId, ct);

            var method = decision.Mode == DeliveryMode.SignalRSilent
                ? "ReceiveChannelMessageSilent"
                : "ReceiveChannelMessage";

            await hubContext.Clients
                .Clients(connectionIds)
                .SendAsync(method, payload, ct);
        }

        if (decision.Mode == DeliveryMode.SignalRAndPush)
        {
            await pushProvider.SendPushAsync(ctx.UserId, payload, ct);
        }
    }
}