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

namespace codeTalks.Application.Features.Channels.Queries.GetAll;

public class GetAllQuery : IRequest<ChannelsByUserIdListModel>
{
    public ChannelUserStatus? Status { get; set; }
    public string? Title { get; set; }
    public int Size { get; set; }
    public int Index { get; set; }

    public class GetAllQueryHandler(
        ICurrentUserService currentUserService,
        IChannelRepository channelRepository,
        IMapper mapper,
        AuthBusinessRules authBusinessRules) : IRequestHandler<GetAllQuery, ChannelsByUserIdListModel>
    {
        public async Task<ChannelsByUserIdListModel> Handle(GetAllQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            await authBusinessRules.CheckUserExistsById(currentUserId);

            IPaginate<Channel> channelsOfUserWhoAccepted = await FilterChannelsByDefinedChannelUserStatusAndRetrieve(request, currentUserId, cancellationToken);
            
            var mappedChannels = mapper.Map<ChannelsByUserIdListModel>(channelsOfUserWhoAccepted);

            foreach (var channelsByUserIdItemDto in mappedChannels.Items)
            {
                var channel = channelsOfUserWhoAccepted.Items
                    .First(channel => channel.Id == channelsByUserIdItemDto.Id);

                var currentUserMembership = channel.ChannelUsers
                    .FirstOrDefault(channelUser => channelUser.UserId == currentUserId);

                channelsByUserIdItemDto.MemberCount = channel.ChannelUsers
                    .Count(x => x.Status == (request.Status ?? ChannelUserStatus.Accepted));

                channelsByUserIdItemDto.Status = currentUserMembership?.Status;
                channelsByUserIdItemDto.Role = currentUserMembership is null
                    ? null
                    : mapper.Map<ChannelsByUserIdRoleDto>(currentUserMembership.Role);
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
        private async Task<IPaginate<Channel>> FilterChannelsByDefinedChannelUserStatusAndRetrieve(GetAllQuery request,
            string currentUserId,
            CancellationToken cancellationToken)
        {
            IPaginate<Channel> channelsOfUserOfDesiredStatus;
            if (request.Status.HasValue)
            {
                channelsOfUserOfDesiredStatus = await channelRepository.GetListAsync(
                    predicate: channel => channel.ChannelUsers.Any(user =>
                        user.UserId == currentUserId && user.Status == request.Status) &&
                        (string.IsNullOrWhiteSpace(request.Title) || channel.Name.ToLower().Contains(request.Title!.ToLower())),
                    orderBy: queryable => queryable.OrderBy(channel => channel.Name),
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
                    predicate: channel => channel.ChannelUsers.All(user => user.UserId != currentUserId) &&
                                          (string.IsNullOrWhiteSpace(request.Title) || channel.Name.ToLower().Contains(request.Title!.ToLower())),
                    orderBy: queryable => queryable.OrderBy(channel => channel.Name),
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