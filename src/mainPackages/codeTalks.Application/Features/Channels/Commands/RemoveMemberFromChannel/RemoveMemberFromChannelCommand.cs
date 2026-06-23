using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.RemoveMemberFromChannel;

public class RemoveMemberFromChannelCommand : ICommand
{
    public string ChannelId { get; set; }
    public string UserId { get; set; }
    
    public class RemoveMemberFromChannelCommandHandler(
        ICurrentUserService currentUserService,
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        IChannelRepository channelRepository) : ICommandHandler<RemoveMemberFromChannelCommand>
    {
        public async Task<Unit> Handle(RemoveMemberFromChannelCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var moderatorRole = await roleManager.FindByNameAsync("Moderator");
            var ownerRole = await roleManager.FindByNameAsync("Owner");

            var channel = await channelRepository.GetDetailedAsync(
                              include: queryable => queryable
                                  .Include(c => c.ChannelUsers)
                                  .ThenInclude(cu => cu.User)
                                  .Include(c => c.ChannelUsers)
                                  .ThenInclude(cu => cu.Role),
                              predicate: c => c.Id == request.ChannelId,
                              cancellationToken: cancellationToken)
                          ?? throw new EntityNotFoundException("This channel doesn't exist");

            var userToRemove = await userManager.Users
                                   .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                               ?? throw new EntityNotFoundException("This user doesn't exist");

            var currentUserAtChannel = channel.ChannelUsers.FirstOrDefault(cu => cu.UserId == currentUserId)
                                       ?? throw new EntityNotFoundException("You haven't registered this channel yet");

            var targetUserAtChannel = channel.ChannelUsers.FirstOrDefault(cu => cu.UserId == userToRemove.Id)
                                      ?? throw new EntityNotFoundException("This user hasn't registered this channel yet");

            var isCurrentUserHasGotAuthorizationForRemoveTargetUser = 
                currentUserAtChannel.Role.Id == moderatorRole!.Id || 
                currentUserAtChannel.Role.Id == ownerRole!.Id;

            var currentUserRole = currentUserAtChannel.Role;
            var targetUserRole = targetUserAtChannel.Role;
            
            var isSelf = targetUserAtChannel.UserId == currentUserId;

            if(isSelf)
                throw new AuthorizationException("You can't remove yourself from channel");

            if (!isSelf && !isCurrentUserHasGotAuthorizationForRemoveTargetUser)
                throw new AuthorizationException("You have no authorization to remove user from channel");
            
            if (currentUserRole.Id == moderatorRole.Id &&
                (targetUserRole.Id == ownerRole!.Id || targetUserRole.Id == moderatorRole.Id))
                throw new AuthorizationException("As a moderator you can only remove regular members from channel");

            channel.ChannelUsers.Remove(targetUserAtChannel);

            await channelRepository.UpdateAsync(channel);
            return Unit.Value;
        }
    }
}