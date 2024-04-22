using System.Security.Claims;
using codeTalks.Application.Services.Repositories;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.LeaveChannel;

public class LeaveChannelCommand : IRequest
{
    public string ChannelId { get; set; }
    
    public class LeaveChannelCommandHandler(
        IHttpContextAccessor httpContextAccessor,
        RoleManager<Role> roleManager,
        IChannelRepository channelRepository) : IRequestHandler<LeaveChannelCommand>
    {
        public async Task Handle(LeaveChannelCommand request, CancellationToken cancellationToken)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
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
        }
    }
}