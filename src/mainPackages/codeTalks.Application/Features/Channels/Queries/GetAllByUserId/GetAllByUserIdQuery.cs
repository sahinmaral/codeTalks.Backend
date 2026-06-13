using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Models;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.Persistence.Paging;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Queries.GetAllByUserId;

public class GetAllByUserIdQuery : IRequest<ChannelsByUserIdListModel>
{
    public ChannelUserStatus? Status { get; set; }
    public int Size { get; set; }
    public int Index { get; set; }

    public class GetAllByUserIdQueryHandler(
        ICurrentUserService currentUserService,
        IChannelRepository channelRepository,
        IMapper mapper,
        AuthBusinessRules authBusinessRules) : IRequestHandler<GetAllByUserIdQuery, ChannelsByUserIdListModel>
    {
        public async Task<ChannelsByUserIdListModel> Handle(GetAllByUserIdQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            await authBusinessRules.CheckUserExistsById(currentUserId);

            IPaginate<Channel> channelsOfUserWhoAccepted = await FilterChannelsByDefinedChannelUserStatusAndRetrieve(request, currentUserId, cancellationToken);
            
            var mappedChannels = mapper.Map<ChannelsByUserIdListModel>(channelsOfUserWhoAccepted);

            foreach (var channelsByUserIdItemDto in mappedChannels.Items)
            {
                var filteredChannelUser = channelsOfUserWhoAccepted.Items
                    .First(channel => channel.Id == channelsByUserIdItemDto.Id).ChannelUsers
                    .First(channelUser => channelUser.UserId == currentUserId);

                channelsByUserIdItemDto.MemberCount = filteredChannelUser.Channel
                    .ChannelUsers
                    .Where(x => x.Status == request.Status)
                    .ToList()
                    .Count;
                
                channelsByUserIdItemDto.Status = filteredChannelUser.Status;
                channelsByUserIdItemDto.Role = mapper.Map<ChannelsByUserIdRoleDto>(filteredChannelUser.Role);
            }

            return mappedChannels;
        }

        /// <summary>
        /// If ChannelUserStatus has been set, filter by also this status with user id. Otherwise, filter by only user id. After that retrieve this paginated channel.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="currentUserId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Paginated Channel as IPaginate</returns>
        private async Task<IPaginate<Channel>> FilterChannelsByDefinedChannelUserStatusAndRetrieve(GetAllByUserIdQuery request,
            string currentUserId,
            CancellationToken cancellationToken)
        {
            IPaginate<Channel> channelsOfUserOfDesiredStatus;
            if (request.Status.HasValue)
            {
                channelsOfUserOfDesiredStatus = await channelRepository.GetListAsync(
                    predicate: channel => channel.ChannelUsers.Any(user =>
                        user.UserId == currentUserId && user.Status == request.Status),
                    include: queryable => queryable
                        .Include(channel => channel.ChannelUsers)
                        .ThenInclude(channelUser => channelUser.Role),
                    size: request.Size,
                    index: request.Index,
                    cancellationToken: cancellationToken);
            }
            else
            {
                channelsOfUserOfDesiredStatus = await channelRepository.GetListAsync(
                    predicate: channel => channel.ChannelUsers.Any(user =>
                        user.UserId == currentUserId),
                    include: queryable => queryable
                        .Include(channel => channel.ChannelUsers)
                        .ThenInclude(channelUser => channelUser.Role),
                    size: request.Size,
                    index: request.Index,
                    cancellationToken: cancellationToken);
            }

            return channelsOfUserOfDesiredStatus;
        }
    }
}