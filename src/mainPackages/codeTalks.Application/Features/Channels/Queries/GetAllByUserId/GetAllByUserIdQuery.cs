using AutoMapper;
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Models;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Queries.GetAllByUserId;

public class GetAllByUserIdQuery : IRequest<ChannelsByUserIdListModel>
{
    public string UserId { get; set; }
    public int Size { get; set; }
    public int Index { get; set; }
    
    public class GetAllByUserIdQueryHandler(IChannelRepository channelRepository, IMapper mapper, AuthBusinessRules authBusinessRules) : IRequestHandler<GetAllByUserIdQuery, ChannelsByUserIdListModel>
    {
        public async Task<ChannelsByUserIdListModel> Handle(GetAllByUserIdQuery request, CancellationToken cancellationToken)
        {
            await authBusinessRules.CheckUserExistsById(request.UserId);

            var channelsOfUserWhoAccepted = await channelRepository.GetListAsync(
                predicate: channel =>
                channel.ChannelUsers
                    .Where(user => user.Status == ChannelUserStatus.Accepted)
                    .Select(user => user.UserId).Contains(request.UserId), 
                include: queryable => queryable
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.Role), 
                size: request.Size,
                index: request.Index,
                cancellationToken: cancellationToken);

            var mappedChannels = mapper.Map<ChannelsByUserIdListModel>(channelsOfUserWhoAccepted);

            foreach (var channelsByUserIdItemDto in mappedChannels.Items)
            {
                channelsByUserIdItemDto.Role = mapper.Map<ChannelsByUserIdRoleDto>(channelsOfUserWhoAccepted.Items
                    .First(channel => channel.Id == channelsByUserIdItemDto.Id).ChannelUsers.First(channelUser => channelUser.UserId == request.UserId).Role);
            }

            return mappedChannels;
        }   
    }
}