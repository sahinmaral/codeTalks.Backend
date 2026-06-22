
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Services;
using codeTalks.Application.Services.FileStorage;
using Core.Application.CQRS;
using Core.Security.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Users.Commands.UpdateProfilePhoto;

public class UpdateProfilePhotoCommand : ICommand<UpdatedProfilePhotoDto>
{
    public IFormFile Image { get; set; }
    
    public class UpdateProfilePhotoCommandHandler(
        ICloudinaryService cloudinaryService,
        ICurrentUserService currentUserService, 
        UserManager<User> userManager,
        AuthBusinessRules authBusinessRules) : ICommandHandler<UpdateProfilePhotoCommand, UpdatedProfilePhotoDto>
    {
        public async Task<UpdatedProfilePhotoDto> Handle(UpdateProfilePhotoCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            User user = await authBusinessRules.CheckUserExistsById(currentUserId);
            
            if (user.ProfilePhotoURL is not null)
            {
                await cloudinaryService.DeleteImageAsync(
                    FileStorageHelpers.ConvertPhotoPathToPublicId(user.ProfilePhotoURL), 
                    cancellationToken);
            }

            var uploadedImageResult = await cloudinaryService.UploadImageAsync(request.Image, cancellationToken);
            string uploadedImagePath = uploadedImageResult.SecureUrl.ToString();

            user.ProfilePhotoURL = uploadedImagePath;

            await userManager.UpdateAsync(user);

            return new UpdatedProfilePhotoDto { NewProfilePhotoPath = uploadedImagePath };
        }
    }
}