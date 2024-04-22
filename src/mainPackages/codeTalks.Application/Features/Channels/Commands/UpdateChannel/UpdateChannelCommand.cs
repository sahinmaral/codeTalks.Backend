using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Features.Channels.Rules;
using codeTalks.Application.Services.Repositories;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.UpdateChannel;

public class UpdateChannelCommand : IRequest
{
    public UpdateChannelDto UpdateChannelDto { get; set; }
    
    public class UpdateChannelCommandHandler(
        IChannelRepository channelRepository,
        RoleManager<Role> roleManager,
        AuthBusinessRules authBusinessRules) : IRequestHandler<UpdateChannelCommand>
    {
        public async Task Handle(UpdateChannelCommand request, CancellationToken cancellationToken)
        {
            await authBusinessRules.CheckUserExistsById(request.UpdateChannelDto.UserId);
            var moderatorRole = await roleManager.FindByNameAsync("Moderator");
            
            var channel = await channelRepository.GetDetailedAsync(
                include: queryable => queryable
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.User)
                    .Include(channel => channel.ChannelUsers)
                    .ThenInclude(channelUser => channelUser.Role),
                predicate: channel => channel.Id == request.UpdateChannelDto.Id
            );

            if (channel is null)
                throw new EntityNotFoundException("This channel doesn't exist");
            
            var foundUserAtChannel = channel.ChannelUsers.FirstOrDefault(channelUser => channelUser.UserId == request.UpdateChannelDto.UserId);
            
            if (foundUserAtChannel is null)
                throw new EntityNotFoundException("This user hasn't registered this channel yet");

            if (foundUserAtChannel.Role.Id != moderatorRole.Id)
                throw new AuthorizationException("You have no authorization to update channel information");
            
            channel.Name = request.UpdateChannelDto.Name ?? channel.Name;
            channel.Description = request.UpdateChannelDto.Description ?? channel.Description;

            await channelRepository.UpdateAsync(channel);
        }
    }
}