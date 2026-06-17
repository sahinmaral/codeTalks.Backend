using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.ChangeUserRole;

public class ChangeUserRoleCommand : ICommand
{
    public string ChannelId { get; set; }
    public string UserId { get; set; }
    public string Role { get; set; }

    public class ChangeUserRoleCommandHandler(
        IChannelRepository channelRepository,
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        ICurrentUserService currentUserService) : ICommandHandler<ChangeUserRoleCommand>
    {
        public async Task<Unit> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var ownerRole = await roleManager.FindByNameAsync("Owner");

            var targetRole = await roleManager.FindByNameAsync(request.Role)
                             ?? throw new EntityNotFoundException("This role doesn't exist");

            var channel = await channelRepository.GetDetailedAsync(
                              include: queryable => queryable
                                  .Include(c => c.ChannelUsers)
                                  .ThenInclude(cu => cu.Role),
                              predicate: c => c.Id == request.ChannelId,
                              cancellationToken: cancellationToken)
                          ?? throw new EntityNotFoundException("This channel doesn't exist");

            var userToUpdate = await userManager.Users
                                   .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                               ?? throw new EntityNotFoundException("This user doesn't exist");

            var targetUserAtChannel = channel.ChannelUsers.FirstOrDefault(cu => cu.UserId == userToUpdate.Id)
                                      ?? throw new EntityNotFoundException("This user hasn't registered this channel yet");

            var currentUserAtChannel = channel.ChannelUsers.FirstOrDefault(cu => cu.UserId == currentUserId)
                                       ?? throw new EntityNotFoundException("You haven't registered this channel yet");

            if (currentUserAtChannel.Role.Id != ownerRole!.Id)
                throw new AuthorizationException("Only the channel owner can change member roles");

            if (targetUserAtChannel.UserId == currentUserAtChannel.UserId)
                throw new AuthorizationException("You can't change your own role");

            if (targetUserAtChannel.Role.Id == ownerRole.Id)
                throw new AuthorizationException("You can't change the role of the channel owner");

            targetUserAtChannel.RoleId = targetRole.Id;

            if (targetRole.Id == ownerRole.Id)
            {
                var userRole = await roleManager.FindByNameAsync("User");
                
                currentUserAtChannel.RoleId = userRole!.Id; 
            }

            await channelRepository.UpdateAsync(channel);

            return Unit.Value;
        }
    }
}
