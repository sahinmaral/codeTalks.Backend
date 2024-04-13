using codeTalks.Application.Features.Messages.Commands.CreateMessage;
using codeTalks.Application.Features.Messages.Queries.GetAllByChannelId;
using codeTalks.Presentation.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace codeTalks.Presentation.Controllers;

public class MessagesController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> CreateMessage([FromBody] CreateMessageCommand request)
    {
        await mediator.Send(request);
        return Ok();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMessagesByChannelId([FromQuery] string channelId)
    {
        GetAllByChannelIdQuery request = new()
        {
            ChannelId = channelId
        };
        var response = await mediator.Send(request);
        return Ok(response);
    }
}