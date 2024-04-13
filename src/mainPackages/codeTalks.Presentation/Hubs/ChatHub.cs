using codeTalks.Application.Features.Channels.Queries.GetAllByUserId;
using codeTalks.Application.Features.Messages.Queries.GetAllByChannelId;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace codeTalks.Presentation.Hubs;

public class ChatHub(IMediator mediator) : Hub
{
    public async Task SendAllChannels(string userId)
    {
        GetAllByUserIdQuery request = new()
        {
            UserId = userId
        };
        var response = await mediator.Send(request);
        
        await Clients.All.SendAsync("ReceiveAllChannels", response);
    }
    
    public async Task SendMessagesOfChannel(string channelId)
    {
        GetAllByChannelIdQuery request = new()
        {
            ChannelId = channelId
        };
        var response = await mediator.Send(request);
        
        await Clients.All.SendAsync("ReceiveMessagesOfChannel", response);
    }
}