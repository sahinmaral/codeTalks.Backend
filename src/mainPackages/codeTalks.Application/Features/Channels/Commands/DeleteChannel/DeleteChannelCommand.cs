using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.DeleteChannel;

public class DeleteChannelCommand : ICommand
{
    public string ChannelId { get; set; }
    
    public class DeleteChannelCommandHandler(
        ICurrentUserService currentUserService,
        RoleManager<Role> roleManager,
        IChannelRepository channelRepository) : ICommandHandler<DeleteChannelCommand>
    {
        public async Task<Unit> Handle(DeleteChannelCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            
            var moderatorRole = await roleManager.FindByNameAsync("Moderator");
            
            var channel = await channelRepository.GetDetailedAsync(
                include: queryable => queryable
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.User)
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.Role),
                predicate: channel => channel.Id == request.ChannelId, cancellationToken: cancellationToken);
            
            if (channel is null)
                throw new EntityNotFoundException("This channel doesn't exist");
            
            var foundUserAtChannel = channel.ChannelUsers.FirstOrDefault(channelUser => channelUser.UserId == currentUserId);
            
            if (foundUserAtChannel is null)
                throw new EntityNotFoundException("This user hasn't registered this channel yet");
            
            if (foundUserAtChannel.Role.Id != moderatorRole!.Id)
                throw new AuthorizationException("You have no authorization to delete channel");
            
            channel.IsActive = false;
            channel.DeletedAt = DateTime.UtcNow;

            await channelRepository.UpdateAsync(channel);
            return Unit.Value;
        }
    }
}