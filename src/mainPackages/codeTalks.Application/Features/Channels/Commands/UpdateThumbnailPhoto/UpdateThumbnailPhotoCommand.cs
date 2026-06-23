using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.FileStorage;
using codeTalks.Application.Services.Repositories;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Channels.Commands.UpdateThumbnailPhoto;

public class UpdateThumbnailPhotoCommand : ICommand<UpdatedThumbnailPhotoDto>
{
    public string ChannelId { get; set; }
    public IFormFile Image { get; set; }
    
    public class UpdateThumbnailPhotoCommandHandler(
        ICloudinaryService cloudinaryService,
        ICurrentUserService currentUserService, 
        RoleManager<Role> roleManager,
        IChannelRepository channelRepository) : ICommandHandler<UpdateThumbnailPhotoCommand, UpdatedThumbnailPhotoDto>
    {
        public async Task<UpdatedThumbnailPhotoDto> Handle(UpdateThumbnailPhotoCommand request,
            CancellationToken cancellationToken)
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
                throw new AuthorizationException("You have no authorization to update channel's thumbnail photo");
            
            if (channel.ThumbnailPhotoURL is not null)
            {
                await cloudinaryService.DeleteImageAsync(
                    FileStorageHelpers.ConvertPhotoPathToPublicId(channel.ThumbnailPhotoURL), 
                    cancellationToken);
            }
            
            var uploadedImageResult = await cloudinaryService.UploadImageAsync(request.Image, cancellationToken);
            string uploadedImagePath = uploadedImageResult.SecureUrl.ToString();
            
            channel.ThumbnailPhotoURL = uploadedImagePath;
            
            await channelRepository.UpdateAsync(channel);

            return new UpdatedThumbnailPhotoDto { NewThumbnailPhotoPath = uploadedImagePath };
        }
    }
}