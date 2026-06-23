using codeTalks.Application.Features.Channels.Queries.GetAll;
using codeTalks.Application.Features.Messages.Queries.GetAllByChannelId;
using codeTalks.Domain;
using codeTalks.Presentation.Hubs.Models;
using Core.Application.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace codeTalks.Presentation.Hubs;

public class ChatHub(IDispatcher dispatcher) : Hub
{
    [Authorize]
    public async Task SendActiveChannelsByUserId(ChannelPageRequest request)
    {
        GetAllQuery getAllByUserIdQuery = new()
        {
            Size = request.PageSize,
            Index = request.Page - 1,
            Status = ChannelUserStatus.Accepted
        };
        var response = await dispatcher.SendAsync(getAllByUserIdQuery);

        await Clients.Caller.SendAsync("ReceiveActiveChannelsByUserId", response);
    }
    
    [Authorize]
    public async Task SendAllChannelsByUserId(ChannelPageRequest request)
    {
        GetAllQuery getAllByUserIdQuery = new()
        {
            Size = request.PageSize,
            Index = request.Page - 1,
        };
        var response = await dispatcher.SendAsync(getAllByUserIdQuery);

        await Clients.Caller.SendAsync("ReceiveAllChannelsByUserId", response);
    }

    [Authorize]
    public async Task SendMessagesOfChannel(string channelId, int? size = null, int? index = null)
    {
        GetAllByChannelIdQuery request = new()
        {
            ChannelId = channelId,
            Size = size ?? 10,
            Index = index ?? 0
        };
        var response = await dispatcher.SendAsync(request);
        
        await Clients.Caller.SendAsync("ReceiveMessagesOfChannel", response);
    }
}