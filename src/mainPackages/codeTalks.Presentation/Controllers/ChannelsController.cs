using codeTalks.Application.Features.Channels.Commands.CreateChannel;
using codeTalks.Application.Features.Channels.Commands.DeleteChannel;
using codeTalks.Application.Features.Channels.Commands.LeaveChannel;
using codeTalks.Application.Features.Channels.Commands.SendInviteToChannel;
using codeTalks.Application.Features.Channels.Commands.UpdateChannel;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Queries.GetAllByUserId;
using codeTalks.Application.Features.Channels.Queries.GetById;
using codeTalks.Application.Features.Channels.Queries.GetUsersDetailAtChannelByChannelId;
using codeTalks.Domain;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

public class ChannelsController : BaseController
{
    [HttpGet("{channelId}")]
    [Authorize]
    public async Task<IActionResult> GetChannelById([FromRoute] string channelId)
    {
        GetByIdQuery request = new()
        {
            ChannelId = channelId,
        };
        var response = await Dispatcher.SendAsync(request);
        return Ok(response);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelCommand request)
    {
        await Dispatcher.SendAsync(request);
        return NoContent();
    }
    
    [Authorize]
    [HttpPost("send-invite/{channelId}")]
    public async Task<IActionResult> SendInviteToChannel([FromRoute] string channelId)
    {
        SendInviteToChannelCommand request = new SendInviteToChannelCommand
        {
            ChannelId = channelId
        };
        await Dispatcher.SendAsync(request);
        return NoContent();
    }
    
    [Authorize]
    [HttpPost("leave/{channelId}")]
    public async Task<IActionResult> LeaveChannel([FromRoute] string channelId)
    {
        
        LeaveChannelCommand request = new LeaveChannelCommand
        {
            ChannelId = channelId
        };
        await Dispatcher.SendAsync(request);
        return NoContent();
    }
    
    [Authorize]
    [HttpDelete("{channelId}")]
    public async Task<IActionResult> DeleteChannel([FromRoute] string channelId)
    {
        DeleteChannelCommand request = new DeleteChannelCommand
        {
            ChannelId = channelId
        };
        await Dispatcher.SendAsync(request);
        return NoContent();
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateChannel([FromBody] UpdateChannelDto dto)
    {
        UpdateChannelCommand request = new UpdateChannelCommand
        {
            UpdateChannelDto = dto
        };
        await Dispatcher.SendAsync(request);
        return NoContent();
    }
    
    [HttpGet("{channelId}/users/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUsersDetailAtChannelByChannelAndUserId([FromRoute] string channelId, [FromRoute] string userId)
    {
        GetUsersDetailAtChannelByChannelIdQuery request = new()
        {
            ChannelId = channelId,
            UserId = userId
        };
        var response = await Dispatcher.SendAsync(request);
        return Ok(response);
    }
}