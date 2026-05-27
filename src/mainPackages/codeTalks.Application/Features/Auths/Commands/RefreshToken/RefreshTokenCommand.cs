using codeTalks.Application.Features.Auths.Dtos;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Core.Security.JWT;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace codeTalks.Application.Features.Auths.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<RefreshedTokenDto>
{
    public string UserId { get; set; }
    public string RefreshToken { get; set; }
    
    public class RefreshTokenCommandHandler(UserManager<User> userManager, IJwtProvider jwtProvider)
        : IRequestHandler<RefreshTokenCommand, RefreshedTokenDto>
    {
        public async Task<RefreshedTokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            User? user = await userManager.FindByIdAsync(request.UserId) 
                         ?? throw new EntityNotFoundException("User could not found");

            if (user.RefreshToken != request.RefreshToken)
                throw new SecurityTokenException("Refresh token is invalid");

            if (user.RefreshTokenExpires < DateTime.Now)
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