using codeTalks.Application.Features.Channels.Commands;
using codeTalks.Application.Features.Channels.Commands.CreateChannel;
using codeTalks.Application.Features.Channels.Commands.UpdateChannel;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Queries.GetAllByUserId;
using codeTalks.Application.Features.Channels.Queries.GetUsersDetailAtChannelByChannelId;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

public class ChannelsController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelCommand request)
    {
        await mediator.Send(request);
        return NoContent();
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateChannel([FromBody] UpdateChannelDto dto)
    {
        UpdateChannelCommand request = new UpdateChannelCommand
        {
            UpdateChannelDto = dto
        };
        await mediator.Send(request);
        return NoContent();
    }
    
    [HttpGet("userDetail/{channelId}/{userId}")]
    public async Task<IActionResult> GetUsersDetailAtChannelByChannelAndUserId([FromRoute] string channelId, [FromRoute] string userId)
    {
        GetUsersDetailAtChannelByChannelIdQuery request = new()
        {
            ChannelId = channelId,
            UserId = userId
        };
        var response = await mediator.Send(request);
        return Ok(response);
    }
    
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetChannelsByUserId([FromQuery] string userId, [FromQuery] int size = 10, [FromQuery] int index = 0)
    {
        GetAllByUserIdQuery request = new()
        {
            UserId = userId,
            Size = size,
            Index = index
        };
        var response = await mediator.Send(request);
        return Ok(response);
    }
}