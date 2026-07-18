using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace codeTalks.Infrastructure.Hubs;

[Authorize]
public class NotificationHub(
    IConnectionTracker connectionTracker,
    ICurrentUserService currentUserService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = await currentUserService.GetCurrentUserIdAsync();
        await connectionTracker.TrackAsync(userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = await currentUserService.GetCurrentUserIdAsync();
        await connectionTracker.UntrackAsync(userId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}