using MapsterMapper;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.Application.CQRS;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Auths.Commands.RegisterUser;

public class RegisterUserCommand : IRequest<RegisteredUserDto>
{
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public string? Bio { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    public sealed class RegisterUserCommandHandler(
        UserManager<User> userManager,
        IUserStatusRepository userStatusRepository,
        AuthBusinessRules authBusinessRules,
        IMapper mapper)
        : IRequestHandler<RegisterUserCommand, RegisteredUserDto>
    {
        public async Task<RegisteredUserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            await authBusinessRules.CheckUserWithUsernameAlreadyExists(request.UserName);
            await authBusinessRules.CheckUserWithEmailAlreadyExists(request.Email);

            User newUser = mapper.Map<User>(request);

            await userManager.CreateAsync(newUser, request.Password);

            UserStatus userStatusForNewUser = new UserStatus
            {
                UserId = newUser.Id,
                Status = UserStatusType.Online
            };
            
            userStatusRepository.Add(userStatusForNewUser);
                
            return mapper.Map<RegisteredUserDto>(newUser);
        }
    }
}