using codeTalks.Application.Features.Auths.Dtos;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Core.Security.JWT;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace codeTalks.Application.Features.Auths.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<RefreshedTokenDto>
{
    public string UserId { get; set; }
    public string RefreshToken { get; set; }
    
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshedTokenDto>
    {
        private readonly UserManager<User> _userManager;
        private readonly IJwtProvider _jwtProvider;

        public RefreshTokenCommandHandler(UserManager<User> userManager, IJwtProvider jwtProvider)
        {
            _userManager = userManager;
            _jwtProvider = jwtProvider;
        }

        public async Task<RefreshedTokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            User? user = await _userManager.FindByIdAsync(request.UserId) 
                         ?? throw new EntityNotFoundException("User could not found");

            if (user.RefreshToken != request.RefreshToken)
                throw new SecurityTokenException("Refresh token is invalid");

            if (user.RefreshTokenExpires < DateTime.Now)
                throw new SecurityTokenException("Refresh token is expired");

            var tokenResponse = await _jwtProvider.CreateTokenAsync(user);

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