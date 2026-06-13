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
    
    public class DeleteChannelCommandHandler(
        ICurrentUserService currentUserService,
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        IChannelRepository channelRepository) : ICommandHandler<RemoveMemberFromChannelCommand>
    {
        public async Task<Unit> Handle(RemoveMemberFromChannelCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var moderatorRole = await roleManager.FindByNameAsync("Moderator");

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

            var isCurrentUserModerator = currentUserAtChannel.Role.Id == moderatorRole!.Id;
            var isLastModerator = channel.ChannelUsers.Count(cu => cu.Role.Id == moderatorRole.Id) == 1;
            var isSelf = targetUserAtChannel.UserId == currentUserId;
            var isLastMember = channel.ChannelUsers.Count == 1;
            
            // isSelf -> true, isModerator -> false ---> That means user leaves channel
            // isSelf -> false, isModerator -> true ---> Current user is moderator and user wants to remove target user from channel

            if (!isSelf && !isCurrentUserModerator)
                throw new AuthorizationException("You have no authorization to remove user from channel");

            if (isSelf && isCurrentUserModerator && isLastModerator && !isLastMember)
                throw new BusinessException("You are the last admin. Transfer ownership before leaving.");

            channel.ChannelUsers.Remove(targetUserAtChannel);

            if (isLastMember)
            {
                channel.IsActive = false;
                channel.DeletedAt = DateTime.UtcNow;
            }

            await channelRepository.UpdateAsync(channel);
            return Unit.Value;
        }
    }
}