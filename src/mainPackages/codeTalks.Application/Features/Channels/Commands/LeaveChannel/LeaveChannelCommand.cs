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
            
            var moderatorRole = await roleManager.FindByNameAsync("Moderator");
            
            var channel = await channelRepository.GetDetailedAsync(
                include: queryable => queryable
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.User)
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.Role),
                predicate: channel => channel.Id == request.ChannelId
            );
            
            if (channel is null)
                throw new EntityNotFoundException("This channel doesn't exist");
            
            var foundUserAtChannel = channel.ChannelUsers.FirstOrDefault(channelUser => channelUser.UserId == currentUserId);
            
            if (foundUserAtChannel is null)
                throw new EntityNotFoundException("This user hasn't registered this channel yet");
            
            if (channel.ChannelUsers.Count == 1 && channel.ChannelUsers.First().Role.Id == moderatorRole.Id)
                throw new AuthorizationException("This user can't leave channel because there's only one moderator left at channel");

            channel.ChannelUsers.Remove(foundUserAtChannel);
            await channelRepository.UpdateAsync(channel);
            return Unit.Value;
        }
    }
}