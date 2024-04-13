using codeTalks.Application.Features.Channels.Commands;
using codeTalks.Application.Features.Channels.Queries.GetAllByUserId;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

public class ChannelsController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelCommand request)
    {
        await mediator.Send(request);
        return Ok();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetChannelsByUserId([FromQuery] string userId)
    {
        GetAllByUserIdQuery request = new()
        {
            UserId = userId
        };
        var response = await mediator.Send(request);
        return Ok(response);
    }
}