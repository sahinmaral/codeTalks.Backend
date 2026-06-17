using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.LeaveChannel;

public class LeaveChannelCommand : ICommand
{
    public string ChannelId { get; set; }
    
    public class LeaveChannelCommandHandler(
        ICurrentUserService currentUserService,
        RoleManager<Role> roleManager,
        IChannelRepository channelRepository) : ICommandHandler<LeaveChannelCommand>
    {
        public async Task<Unit> Handle(LeaveChannelCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            var ownerRole = await roleManager.FindByNameAsync("Owner");
            
            var channel = await channelRepository.GetDetailedAsync(
                include: queryable => queryable
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.User)
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.Role),
                predicate: channel => channel.Id == request.ChannelId, 
                cancellationToken: cancellationToken);
            
            if (channel is null)
                throw new EntityNotFoundException("This channel doesn't exist");
            
            var foundUserAtChannel = channel.ChannelUsers.FirstOrDefault(channelUser => channelUser.UserId == currentUserId);
            
            if (foundUserAtChannel is null)
                throw new EntityNotFoundException("You haven't registered this channel yet");
            
            var isOwner = foundUserAtChannel.Role.Id == ownerRole!.Id;

            if (isOwner)
            {
                if(channel.ChannelUsers.Count >= 2)
                    throw new AuthorizationException("You can't leave channel, you have to transfer your ownership to someone else");
                
                channel.IsActive = false;
                channel.DeletedAt = DateTime.UtcNow;
            }

            channel.ChannelUsers.Remove(foundUserAtChannel);
            await channelRepository.UpdateAsync(channel);
            return Unit.Value;
        }
    }
}