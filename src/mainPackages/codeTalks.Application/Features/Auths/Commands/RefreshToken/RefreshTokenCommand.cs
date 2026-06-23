using System.Security.Claims;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Channels.Dtos;
using Core.Application.CQRS;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Core.Security.JWT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace codeTalks.Application.Features.Auths.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<RefreshedTokenDto>
{
    public string RefreshToken { get; set; }
    
    public class RefreshTokenCommandHandler(
        UserManager<User> userManager, 
        IJwtProvider jwtProvider,
        IHttpContextAccessor httpContextAccessor
        )
        : IRequestHandler<RefreshTokenCommand, RefreshedTokenDto>
    {
        public async Task<RefreshedTokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? throw new InvalidOperationException("User ID claim not found.");
            
            User user = await userManager.FindByIdAsync(userId) 
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