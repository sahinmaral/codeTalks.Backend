using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Services;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Users.Commands.ChangeUserPassword;

public class ChangeUserPasswordCommand : ICommand
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }

    public class ChangeUserPasswordCommandHandler(
        ICurrentUserService currentUserService,
        UserManager<User> userManager,
        AuthBusinessRules authBusinessRules) : ICommandHandler<ChangeUserPasswordCommand>
    {
        public async Task<Unit> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();

            User user = await authBusinessRules.CheckUserExistsById(currentUserId);

            await authBusinessRules.CheckIfUserEnteredCorrectPassword(user, request.CurrentPassword, "Your current password is incorrect");

            IdentityResult result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                throw new BusinessException(string.Join(" ", result.Errors.Select(e => e.Description)));

            return Unit.Value;
        }
    }
}