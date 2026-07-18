using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.PatchUserStatus;

public class PatchUserStatusCommand : ICommand
{
    public string ChannelId { get; set; }
    public string UserId { get; set; }
    public ChannelUserStatus Status { get; set; }
    
    public class PatchUserStatusCommandHandler(
        IChannelRepository channelRepository,
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        ICurrentUserService currentUserService) : ICommandHandler<PatchUserStatusCommand>
    {
        public async Task<Unit> Handle(PatchUserStatusCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var moderatorRole = await roleManager.FindByNameAsync("Moderator");
            var ownerRole = await roleManager.FindByNameAsync("Owner");

            var channel = await channelRepository.GetDetailedAsync(
                include: queryable => queryable
                    .Include(c => c.ChannelUsers)
                    .ThenInclude(cu => cu.Role),
                predicate: c => c.Id == request.ChannelId,
                cancellationToken: cancellationToken
            );

            if (channel is null)
                throw new EntityNotFoundException("This channel doesn't exist");

            if (channel.JoinPolicy == ChannelJoinPolicy.Open &&
                (request.Status == ChannelUserStatus.Accepted ||
                 request.Status == ChannelUserStatus.Denied))
                throw new BusinessException($"You can't set a user's channel status to {request.Status}");

            var userToUpdateChannelUserStatus = await userManager.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken: cancellationToken) 
                                                ?? throw new EntityNotFoundException("This user doesn't exist");

            var foundUserAtChannel = channel.ChannelUsers.FirstOrDefault(cu => cu.UserId == userToUpdateChannelUserStatus.Id);
            var currentUserAtChannel = channel.ChannelUsers.FirstOrDefault(cu => cu.UserId == currentUserId);

            if (foundUserAtChannel is null)
                throw new EntityNotFoundException("This user hasn't registered this channel yet");
            
            if (currentUserAtChannel is null)
                throw new EntityNotFoundException("You haven't registered this channel yet");

            if (currentUserAtChannel.Role.Id != moderatorRole!.Id &&
                currentUserAtChannel.Role.Id != ownerRole!.Id)
                throw new AuthorizationException("You have no authorization to update user's channel status");
            
            if (foundUserAtChannel.UserId == currentUserAtChannel.UserId)
                throw new AuthorizationException("You can't update your own channel status");

            bool isBan    = request.Status == ChannelUserStatus.Banned;
            bool isUnban  = request.Status == ChannelUserStatus.Accepted &&
                            foundUserAtChannel.Status == ChannelUserStatus.Banned;

            if (isBan || isUnban)
            {
                bool currentUserIsOwner = currentUserAtChannel.Role.Id == ownerRole!.Id;
                
                if (!currentUserIsOwner)
                {
                    if (foundUserAtChannel.Role.Id == ownerRole.Id)
                        throw new AuthorizationException($"You can't {(isBan ? "ban" : "unban")} owner");
                    if (foundUserAtChannel.Role.Id == moderatorRole!.Id)
                        throw new AuthorizationException($"You can't {(isBan ? "ban" : "unban")} another moderator");
                }
            }
            
            foundUserAtChannel.Status = request.Status;
            
            await channelRepository.UpdateAsync(channel);

            return Unit.Value;
        }
    }
}