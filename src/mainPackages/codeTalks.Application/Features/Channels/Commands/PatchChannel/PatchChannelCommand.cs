using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.PatchChannel;

public class PatchChannelCommand : ICommand
{
    public string ChannelId { get; set; }
    public PatchChannelDto PatchChannelDto { get; set; }

    public class PatchChannelCommandHandler(
        IChannelRepository channelRepository,
        RoleManager<Role> roleManager,
        ICurrentUserService currentUserService) : ICommandHandler<PatchChannelCommand>
    {
        public async Task<Unit> Handle(PatchChannelCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var moderatorRole = await roleManager.FindByNameAsync("Moderator");

            var channel = await channelRepository.GetDetailedAsync(
                include: queryable => queryable
                    .Include(c => c.ChannelUsers)
                    .ThenInclude(cu => cu.Role),
                predicate: c => c.Id == request.ChannelId,
                cancellationToken: cancellationToken
            );

            if (channel is null)
                throw new EntityNotFoundException("This channel doesn't exist");

            var foundUserAtChannel = channel.ChannelUsers.FirstOrDefault(cu => cu.UserId == currentUserId);

            if (foundUserAtChannel is null)
                throw new EntityNotFoundException("This user hasn't registered this channel yet");

            if (foundUserAtChannel.Role.Id != moderatorRole!.Id)
                throw new AuthorizationException("You have no authorization to update channel information");

            channel.Name = request.PatchChannelDto.Name ?? channel.Name;
            channel.Description = request.PatchChannelDto.Description ?? channel.Description;

            await channelRepository.UpdateAsync(channel);
            return Unit.Value;
        }
    }
}