using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.JoinChannel;

public class JoinChannelCommand : ICommand<JoinChannelResult>
{
    public string InviteCode { get; set; }

    public class JoinChannelCommandHandler(
        ICurrentUserService currentUserService,
        RoleManager<Role> roleManager,
        IChannelRepository channelRepository) : ICommandHandler<JoinChannelCommand, JoinChannelResult>
    {
        public async Task<JoinChannelResult> Handle(JoinChannelCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var userRole = await roleManager.FindByNameAsync("User");
            
            var channel = await channelRepository.GetDetailedAsync(
                include: queryable => queryable
                    .Include(channel => channel.ChannelUsers),
                predicate: channel => channel.InviteCode == request.InviteCode,
                cancellationToken: cancellationToken
            );
            
            if (channel is null)
                throw new EntityNotFoundException("This channel doesn't exist");
            
            var foundUserAtChannel = channel.ChannelUsers.FirstOrDefault(channelUser => channelUser.UserId == currentUserId);
            if (foundUserAtChannel?.Status == ChannelUserStatus.Banned)
                throw new BusinessException("You have been banned from this channel");

            if (foundUserAtChannel?.Status == ChannelUserStatus.Accepted)
                throw new BusinessException("You already accepted to this channel");

            if (foundUserAtChannel?.Status == ChannelUserStatus.RequestSent)
                throw new BusinessException("You already sent a request to this channel");

            var userCurrentChannelStatus = channel.JoinPolicy == ChannelJoinPolicy.Open
                ? ChannelUserStatus.Accepted
                : ChannelUserStatus.RequestSent;

            channel.ChannelUsers.Add(new ChannelUser
            {
                UserId = currentUserId,
                RoleId = userRole!.Id,
                Status = userCurrentChannelStatus
            });
            
            await channelRepository.UpdateAsync(channel);
            return new JoinChannelResult
            {
                Status = userCurrentChannelStatus
            };
        }
    }
}