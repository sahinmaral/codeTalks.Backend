using codeTalks.Application.Features.Channels.Commands.LeaveChannel;
using codeTalks.Application.Features.Users.Helpers;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.SendInviteToChannel;

public class SendInviteToChannelCommand : IRequest
{
    public string ChannelId { get; set; }
    
    public class SendInviteToChannelCommandHandler(
        IHttpContextAccessor httpContextAccessor,
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        IChannelRepository channelRepository) : IRequestHandler<SendInviteToChannelCommand>
    {
        public async Task Handle(SendInviteToChannelCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await UserContextHelper.GetCurrentUserId(httpContextAccessor, userManager);
            var userRole = await roleManager.FindByNameAsync("User");
            
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
            if (foundUserAtChannel is not null && foundUserAtChannel.Status == ChannelUserStatus.Accepted)
                throw new BusinessException("This user has already accepted to this channel");

            channel.ChannelUsers.Add(new ChannelUser
            {
                UserId = currentUserId,
                RoleId = userRole.Id,
                Status = ChannelUserStatus.RequestSent
            });
            await channelRepository.UpdateAsync(channel);
        }
    }
}