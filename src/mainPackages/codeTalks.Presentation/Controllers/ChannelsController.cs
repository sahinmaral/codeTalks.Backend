using codeTalks.Application.Features.Channels.Commands.CreateChannel;
using codeTalks.Application.Features.Channels.Commands.DeleteChannel;
using codeTalks.Application.Features.Channels.Commands.LeaveChannel;
using codeTalks.Application.Features.Channels.Commands.PatchChannel;
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
    
    [HttpPut("{channelId}")]
    [Authorize]
    public async Task<IActionResult> UpdateChannel([FromRoute] string channelId, [FromBody] UpdateChannelDto dto)
    {
        UpdateChannelCommand request = new UpdateChannelCommand
        {
            ChannelId = channelId,
            UpdateChannelDto = dto
        };
        await Dispatcher.SendAsync(request);
        return NoContent();
    }

    [HttpPatch("{channelId}")]
    [Authorize]
    public async Task<IActionResult> PatchChannel([FromRoute] string channelId, [FromBody] PatchChannelDto dto)
    {
        PatchChannelCommand request = new PatchChannelCommand
        {
            ChannelId = channelId,
            PatchChannelDto = dto
        };
        await Dispatcher.SendAsync(request);
        return NoContent();
    }

    [HttpGet("{channelId}/users")]
    [Authorize]
    public async Task<IActionResult> GetUsersByChannelId(
        [FromRoute] string channelId,
        [FromQuery] ChannelUserStatus status = ChannelUserStatus.Accepted,
        [FromQuery] string? search = null,
        [FromQuery] int index = 0,
        [FromQuery] int size = 10)
    {
        GetUsersByChannelIdQuery request = new()
        {
            ChannelId = channelId,
            Status = status,
            Search = search,
            Index = index,
            Size = size
        };
        var response = await Dispatcher.SendAsync(request);
        return Ok(response);
    }
}