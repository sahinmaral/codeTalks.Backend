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
        await Dispatcher.SendAsync(request);
        return Ok();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMessagesByChannelId([FromQuery] string channelId, [FromQuery]int size = 10, [FromQuery]int index = 0)
    {
        GetAllByChannelIdQuery request = new()
        {
            ChannelId = channelId,
            Size = size,
            Index = index
        };
        var response = await Dispatcher.SendAsync(request);
        return Ok(response);
    }
}