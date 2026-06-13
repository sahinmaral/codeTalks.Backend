using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.Security.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Auths.Queries.GetCurrentUser;

public class GetCurrentUserQuery : IRequest<GetCurrentUserDto>
{
    public class GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        UserManager<User> userManager,
        IChannelRepository channelRepository,
        IUserStatusRepository userStatusRepository,
        IMapper mapper
    ) : IRequestHandler<GetCurrentUserQuery, GetCurrentUserDto>
    {
        public async Task<GetCurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();

            var user = await userManager.FindByIdAsync(currentUserId)
                ?? throw new InvalidOperationException("User not found.");

            var channelsWhoUserJoined = await channelRepository.GetListAsync(
                predicate: channel => channel.ChannelUsers.Any(cu => cu.UserId == currentUserId),
                cancellationToken: cancellationToken);
            
            var userStatusOfUser = await userStatusRepository.GetAsync(x => x.UserId == currentUserId, cancellationToken);
            if (userStatusOfUser == null) throw new InvalidOperationException("User status not found.");
            
            var userStatus = mapper.Map<GetUserStatusDto>(userStatusOfUser);
            
            GetCurrentUserDto response = mapper.Map<GetCurrentUserDto>(user);
            response.JoinedChannelCount = channelsWhoUserJoined.Count;
            response.UserStatus = userStatus;

            return response;
        }
    }
}