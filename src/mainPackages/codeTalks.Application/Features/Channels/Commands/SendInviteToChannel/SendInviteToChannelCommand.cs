using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.SendInviteToChannel;

public class SendInviteToChannelCommand : ICommand
{
    public string ChannelId { get; set; }
    
    public class SendInviteToChannelCommandHandler(
        ICurrentUserService currentUserService,
        RoleManager<Role> roleManager,
        IChannelRepository channelRepository) : ICommandHandler<SendInviteToChannelCommand>
    {
        public async Task<Unit> Handle(SendInviteToChannelCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var userRole = await roleManager.FindByNameAsync("User");
            
            var channel = await channelRepository.GetDetailedAsync(
                include: queryable => queryable
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.User)
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.Role),
                predicate: channel => channel.InviteCode == request.ChannelId,
                cancellationToken: cancellationToken
            );
            
            if (channel is null)
                throw new EntityNotFoundException("This channel doesn't exist");
            
            var foundUserAtChannel = channel.ChannelUsers.FirstOrDefault(channelUser => channelUser.UserId == currentUserId);
            if (foundUserAtChannel is not null && foundUserAtChannel.Status == ChannelUserStatus.Accepted)
                throw new BusinessException("You already accepted to this channel");
            
            if (foundUserAtChannel is not null && foundUserAtChannel.Status == ChannelUserStatus.Banned)
                throw new BusinessException("You have been banned from this channel");

            channel.ChannelUsers.Add(new ChannelUser
            {
                UserId = currentUserId,
                RoleId = userRole!.Id,
                Status = ChannelUserStatus.RequestSent
            });
            await channelRepository.UpdateAsync(channel);
            return Unit.Value;
        }
    }
}