using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Services;
using codeTalks.Application.Services.FileStorage;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Users.Commands.DeleteProfilePhoto;

public class DeleteProfilePhotoCommand : ICommand
{
    public class DeleteProfilePhotoCommandHandler(
        ICloudinaryService cloudinaryService,
        ICurrentUserService currentUserService, 
        UserManager<User> userManager,
        AuthBusinessRules authBusinessRules) : ICommandHandler<DeleteProfilePhotoCommand>
    {
        public async Task<Unit> Handle(DeleteProfilePhotoCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            User user = await authBusinessRules.CheckUserExistsById(currentUserId);
            
            if (user.ProfilePhotoURL is null)
                throw new BusinessException("You haven't uploaded any profile photo yet");
            
            await cloudinaryService.DeleteImageAsync(
                FileStorageHelpers.ConvertPhotoPathToPublicId(user.ProfilePhotoURL), 
                cancellationToken);

            user.ProfilePhotoURL = null;

            await userManager.UpdateAsync(user);
            
            return Unit.Value;
        }
    }
}