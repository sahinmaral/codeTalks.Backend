using codeTalks.Application.Features.Channels.Queries.GetAllByUserId;
using codeTalks.Application.Features.Messages.Queries.GetAllByChannelId;
using codeTalks.Domain;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace codeTalks.Presentation.Hubs;

public class ChatHub(IMediator mediator) : Hub
{
    public async Task SendActiveChannelsByUserId(string userId, int? size = null, int? index = null)
    {
        GetAllByUserIdQuery request = new()
        {
            UserId = userId,
            Size = size ?? 10,
            Index = index ?? 0,
            Status = ChannelUserStatus.Accepted
        };
        var response = await mediator.Send(request);
        
        await Clients.All.SendAsync("ReceiveActiveChannelsByUserId", response);
    }
    
    public async Task SendAllChannelsByUserId(string userId, int? size = null, int? index = null)
    {
        GetAllByUserIdQuery request = new()
        {
            UserId = userId,
            Size = size ?? 10,
            Index = index ?? 0
        };
        var response = await mediator.Send(request);
        
        await Clients.All.SendAsync("ReceiveAllChannelsByUserId", response);
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