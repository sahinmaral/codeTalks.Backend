using AutoMapper;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Auths.Rules;
using Core.Security.Entities;
using Core.Security.JWT;
using MediatR;

namespace codeTalks.Application.Features.Auths.Commands.LoginUser;

public class LoginUserCommand : IRequest<LoggedUserDto>
{
    public string UsernameOrEmail { get; set; }
    public string Password { get; set; }

    public class LoginUserCommandHandler(
        AuthBusinessRules authBusinessRules,
        IJwtProvider jwtProvider,
        IMapper mapper)
        : IRequestHandler<LoginUserCommand, LoggedUserDto>
    {
        public async Task<LoggedUserDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            User loggedInUser = await authBusinessRules.CheckUserWithEmailOrUsernameExists(request.UsernameOrEmail);

            await authBusinessRules.CheckIfUserEnteredCorrectPassword(loggedInUser, request.Password);

            //TODO: E-posta doğrulaması yerine telefon numarası doğrulaması yapılabilir
            //await _authBusinessRules.CheckIfUserEmailHasBeenVerified(loggedInUser);

            TokenResponse tokenResponse = await jwtProvider.CreateTokenAsync(loggedInUser);

            LoggedUserDto loggedUserDto = mapper.Map<LoggedUserDto>(loggedInUser);
            loggedUserDto.AccessToken = tokenResponse.AccessToken;
            loggedUserDto.RefreshToken = tokenResponse.RefreshToken;
            loggedUserDto.RefreshTokenExpires = tokenResponse.RefreshTokenExpires;

            return loggedUserDto;
        }
    }
}
