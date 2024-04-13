using AutoMapper;
using codeTalks.Application.Features.Channels.Rules;
using codeTalks.Application.Features.Users.Models;
using codeTalks.Application.Services.Repositories;
using Core.Persistence.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Users.Query.GetAllByChannelId;

public class GetAllByChannelIdQuery : IRequest<UsersByChannelIdListModel>
{
    public string ChannelId { get; set; }
    public int Size { get; set; }
    public int Index { get; set; }
    
    public class GetAllByChannelIdQueryHandler(IChannelRepository channelRepository, ChannelBusinessRules channelBusinessRules, IMapper mapper) : IRequestHandler<GetAllByChannelIdQuery,UsersByChannelIdListModel>
    {
        public async Task<UsersByChannelIdListModel> Handle(GetAllByChannelIdQuery request, CancellationToken cancellationToken)
        {
            await channelBusinessRules.CheckChannelExistsById(request.ChannelId);
            var channel = await channelRepository.GetDetailedAsync(predicate: channel => channel.Id == request.ChannelId,
                include: queryable => queryable
                        .Include(channel => channel.ChannelUsers)
                            .ThenInclude(channelUser => channelUser.User)
                        .Include(channel => channel.ChannelUsers)
                            .ThenInclude(channelUser => channelUser.Role)
            );

            var channelUsers = channel.ChannelUsers.AsQueryable().ToPaginate(request.Index, request.Size);

            return mapper.Map<UsersByChannelIdListModel>(channelUsers);
        }
    }
}