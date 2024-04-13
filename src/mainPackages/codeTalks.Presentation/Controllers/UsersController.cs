using codeTalks.Application.Features.Users.Query.GetAllByChannelId;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

public class UsersController : BaseController
{
    
    [HttpGet]
    public async Task<IActionResult> GetUsersByChannelId([FromQuery] string channelId, [FromQuery] int size = 10, [FromQuery] int index = 0)
    {
        GetAllByChannelIdQuery request = new()
        {
            ChannelId = channelId,
            Index = index,
            Size = size
        };
        var response = await mediator.Send(request);
        return Ok(response);
    }
}