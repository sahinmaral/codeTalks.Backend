using System.Security.Claims;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Users.Helpers;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.Security.Entities;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Auths.Queries.GetCurrentUser;

public class GetCurrentUserQuery : IRequest<GetCurrentUserDto>
{
    public class GetCurrentUserQueryHandler(
        UserManager<User> userManager,
        IChannelRepository channelRepository,
        IUserStatusRepository userStatusRepository,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper
    ) : IRequestHandler<GetCurrentUserQuery, GetCurrentUserDto>
    {
        public async Task<GetCurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = await UserContextHelper.GetCurrentUserId(httpContextAccessor, userManager);

            var user = await userManager.FindByIdAsync(currentUserId)
                ?? throw new InvalidOperationException("User not found.");

            var channelsWhoUserJoined = await channelRepository.GetListAsync(channel =>
                    channel.ChannelUsers.FirstOrDefault(channelUser => channelUser.UserId == currentUserId) != null,
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