using codeTalks.Application.Features.Auths.Dtos;
using Core.Application.CQRS;
using Core.Security.Entities;
using Core.Security.JWT;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace codeTalks.Application.Features.Auths.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<RefreshedTokenDto>
{
    public string RefreshToken { get; set; }

    public class RefreshTokenCommandHandler(
        UserManager<User> userManager,
        IJwtProvider jwtProvider
        )
        : IRequestHandler<RefreshTokenCommand, RefreshedTokenDto>
    {
        public async Task<RefreshedTokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            User user = await userManager.Users
                            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken)
                        ?? throw new SecurityTokenException("Refresh token is invalid");

            if (user.RefreshTokenExpires < DateTime.UtcNow)
                throw new SecurityTokenException("Refresh token is expired");

            var tokenResponse = await jwtProvider.CreateTokenAsync(user);

            RefreshedTokenDto refreshedTokenDto = new RefreshedTokenDto
            {
                RefreshToken = tokenResponse.RefreshToken,
                AccessToken = tokenResponse.AccessToken,
                RefreshTokenExpires = tokenResponse.RefreshTokenExpires
            };

            return refreshedTokenDto;
        }
    }
}