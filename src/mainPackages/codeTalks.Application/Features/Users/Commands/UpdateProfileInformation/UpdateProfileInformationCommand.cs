using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Services;
using Core.Application.CQRS;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Users.Commands.UpdateProfileInformation;

public class UpdateProfileInformationCommand : ICommand
{
    public UpdateProfileInformationDto ProfileInformation { get; set; }
    
    public class UpdateProfileInformationCommandCommandHandler(
        ICurrentUserService currentUserService, 
        UserManager<User> userManager,
        AuthBusinessRules authBusinessRules) : ICommandHandler<UpdateProfileInformationCommand>
    {
        public async Task<Unit> Handle(UpdateProfileInformationCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            User user = await authBusinessRules.CheckUserExistsById(currentUserId);
            
            user.FirstName = request.ProfileInformation.FirstName;
            user.MiddleName = request.ProfileInformation.MiddleName ?? user.MiddleName;
            user.LastName = request.ProfileInformation.LastName;
            user.Bio = request.ProfileInformation.Bio ?? user.Bio;
            
            await userManager.UpdateAsync(user);

            return Unit.Value;
        }
    }
}