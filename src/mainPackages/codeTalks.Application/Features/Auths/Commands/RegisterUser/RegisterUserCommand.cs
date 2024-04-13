using AutoMapper;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Auths.Rules;
using Core.Security.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace codeTalks.Application.Features.Auths.Commands.RegisterUser;

public partial class RegisterUserCommand : IRequest<RegisteredUserDto>
{
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }

    public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisteredUserDto>
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly AuthBusinessRules _authBusinessRules;
        private readonly IMapper _mapper;

        public RegisterUserCommandHandler(UserManager<User> userManager, AuthBusinessRules authBusinessRules, IMapper mapper, RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _authBusinessRules = authBusinessRules;
            _mapper = mapper;
            _roleManager = roleManager;
        }

        public async Task<RegisteredUserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            await _authBusinessRules.CheckUserWithUsernameAlreadyExists(request.UserName);
            await _authBusinessRules.CheckUserWithEmailAlreadyExists(request.Email);

            User newUser = _mapper.Map<User>(request);

            await _userManager.CreateAsync(newUser, request.Password);
            await _userManager.AddToRoleAsync(newUser, "User");
                
            return _mapper.Map<RegisteredUserDto>(newUser);
        }
    }
}