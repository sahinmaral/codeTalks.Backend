using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Services;
using codeTalks.Application.Services.FileStorage;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.DeleteThumbnailPhoto;

public class DeleteThumbnailPhotoCommand : ICommand
{
    public string ChannelId { get; set; }
    
    public class DeleteThumbnailPhotoCommandHandler(
        ICloudinaryService cloudinaryService,
        ICurrentUserService currentUserService,
        IChannelRepository channelRepository,
        RoleManager<Role> roleManager) : ICommandHandler<DeleteThumbnailPhotoCommand>
    {
        public async Task<Unit> Handle(DeleteThumbnailPhotoCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var ownerRole = await roleManager.FindByNameAsync("Owner");

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
                throw new EntityNotFoundException("You haven't registered this channel yet");
            
            if (foundUserAtChannel.Role.Id != ownerRole!.Id)
                throw new AuthorizationException("You have no authorization to delete channel's thumbnail photo");
            
            if (channel.ThumbnailPhotoURL is null)
                throw new BusinessException("You haven't uploaded any profile photo yet");
            
            await cloudinaryService.DeleteImageAsync(
                FileStorageHelpers.ConvertPhotoPathToPublicId(channel.ThumbnailPhotoURL), 
                cancellationToken);

            channel.ThumbnailPhotoURL = null;

            await channelRepository.UpdateAsync(channel);
            
            return Unit.Value;
        }
    }
}