using codeTalks.Application.Features.Users.Commands.UpdateUserStatus;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Features.Users.Query.GetAllByChannelId;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

public class UsersController : BaseController
{
    
    [HttpGet("channels/{channelId}")]
    public async Task<IActionResult> GetUsersByChannelId([FromRoute] string channelId, [FromQuery] int size = 10, [FromQuery] int index = 0)
    {
        GetAllByChannelIdQuery request = new()
        {
            ChannelId = channelId,
            Index = index,
            Size = size
        };
        var response = await Dispatcher.SendAsync(request);
        return Ok(response);
    }

    [Authorize]
    [HttpPut("status")]
    public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusCommand request)
    {
        await Dispatcher.SendAsync(request);
        
        return NoContent();
    }
}