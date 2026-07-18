using MapsterMapper;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Channels.Dtos;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Auths.Commands.RegisterUser;

public class RegisterUserCommand : IRequest<RegisteredUserDto>
{
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    public sealed class RegisterUserCommandHandler(
        UserManager<User> userManager,
        IUserStatusRepository userStatusRepository,
        IUserNotificationSettingRepository userNotificationSettingRepository,
        AuthBusinessRules authBusinessRules,
        IMapper mapper)
        : IRequestHandler<RegisterUserCommand, RegisteredUserDto>
    {
        public async Task<RegisteredUserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            await authBusinessRules.CheckUserWithUsernameAlreadyExists(request.UserName);
            await authBusinessRules.CheckUserWithEmailAlreadyExists(request.Email);

            User newUser = mapper.Map<User>(request);

            IdentityResult result = await userManager.CreateAsync(newUser, request.Password);
            if (!result.Succeeded)
                throw new BusinessException(string.Join(" ", result.Errors.Select(e => e.Description)));

            UserStatus userStatusForNewUser = new UserStatus
            {
                UserId = newUser.Id,
                Status = UserStatusType.Online
            };
            
            userStatusRepository.Add(userStatusForNewUser);

            UserNotificationSetting userNotificationSetting = new UserNotificationSetting
            {
                UserId = newUser.Id,
                IsEnabled = true,
                IsSoundEnabled = false
            };
            
            userNotificationSettingRepository.Add(userNotificationSetting);
                
            return mapper.Map<RegisteredUserDto>(newUser);
        }
    }
}