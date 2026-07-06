using codeTalks.Application.Features.Notifications.Commands;
using codeTalks.Application.Features.Notifications.Queries;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

[Authorize]
public class NotificationsController : BaseController
{
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await Dispatcher.SendAsync(new GetUnreadCountQuery(), ct);
        return Ok(count);
    }
    
    [HttpGet("unread-count/{channelId}")]
    public async Task<IActionResult> GetChannelUnreadCount([FromRoute]string channelId, CancellationToken ct)
    {
        var count = await Dispatcher.SendAsync(new GetChannelUnreadCountQuery
        {
            ChannelId = channelId
        }, ct);
        return Ok(count);
    }
    
    [HttpPost("reset/{channelId}")]
    public async Task<IActionResult> ResetChannelUnreadCount([FromRoute]string channelId, CancellationToken ct)
    {
        await Dispatcher.SendAsync(new ResetChannelUnreadCountCommand
        {
            ChannelId = channelId
        }, ct);
        
        return Ok();
    }
}