using codeTalks.Application.Features.Channels.Queries.GetAllByUserId;
using codeTalks.Application.Features.Messages.Queries.GetAllByChannelId;
using codeTalks.Domain;
using codeTalks.Presentation.Hubs.Models;
using Core.Application.CQRS;
using Core.Application.Requests;
using Microsoft.AspNetCore.SignalR;

namespace codeTalks.Presentation.Hubs;

public class ChatHub(IDispatcher dispatcher) : Hub
{
    public async Task SendActiveChannelsByUserId(ChannelPageRequest request)
    {
        GetAllByUserIdQuery getAllByUserIdQuery = new()
        {
            UserId = request.UserId,
            Size = request.PageSize,
            Index = request.Page - 1,
            Status = ChannelUserStatus.Accepted
        };
        var response = await dispatcher.SendAsync(getAllByUserIdQuery);

        await Clients.Caller.SendAsync("ReceiveActiveChannelsByUserId", response);
    }
    public async Task SendAllChannelsByUserId(string userId, int? size = null, int? index = null)
    {
        GetAllByUserIdQuery request = new()
        {
            UserId = userId,
            Size = size ?? 10,
            Index = index ?? 0
        };
        var response = await dispatcher.SendAsync(request);

        await Clients.All.SendAsync("ReceiveAllChannelsByUserId", response);
    }

    public async Task SendMessagesOfChannel(string channelId)
    {
        GetAllByChannelIdQuery request = new()
        {
            ChannelId = channelId
        };
        var response = await dispatcher.SendAsync(request);

        await Clients.All.SendAsync("ReceiveMessagesOfChannel", response);
    }
}