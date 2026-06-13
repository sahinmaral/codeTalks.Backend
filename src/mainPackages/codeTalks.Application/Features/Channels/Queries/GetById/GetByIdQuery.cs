using codeTalks.Application.Features.Channels.Dtos;
using MapsterMapper;
using codeTalks.Application.Features.Channels.Models;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Queries.GetById;

public class GetByIdQuery : IRequest<ChannelByIdDto>
{
    public string ChannelId { get; set; }

    public class GetByIdQueryHandler(
        IChannelRepository channelRepository,
        ICurrentUserService currentUserService,
        IMapper mapper) : IRequestHandler<GetByIdQuery, ChannelByIdDto>
    {
        public async Task<ChannelByIdDto> Handle(GetByIdQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            var foundChannel = await channelRepository.GetDetailedAsync(
                predicate: x => x.Id == request.ChannelId,
                    include: queryable => queryable
                        .Include(channel => channel.ChannelUsers)
                        .ThenInclude(channelUser => channelUser.User)
                        .Include(channel => channel.ChannelUsers)
                        .ThenInclude(channelUser => channelUser.Role),
                    cancellationToken: cancellationToken
                );
            
            if (foundChannel == null)
                throw new EntityNotFoundException("Channel not found");

            var currentUserChannelUser = foundChannel.ChannelUsers.FirstOrDefault(x =>
                x.UserId == currentUserId && x.Status == ChannelUserStatus.Accepted);
            
            if (currentUserChannelUser is null)
                throw new BusinessException("You are not authorized to see this channel");
            
            var mappedChannel = mapper.Map<ChannelByIdDto>(foundChannel);
            
            mappedChannel.MemberCount = foundChannel.ChannelUsers.Count(x => x.Status == ChannelUserStatus.Accepted);
            mappedChannel.Role = mapper.Map<ChannelsByUserIdRoleDto>(currentUserChannelUser.Role);

            return mappedChannel;
        }
    }
}