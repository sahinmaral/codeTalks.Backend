using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Core.Security.JWT;

public class JwtProvider(IOptions<JwtOptions> jwtOptions, UserManager<User> userManager)
    : IJwtProvider
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<TokenResponse> CreateTokenAsync(User user)
    {
        var claims = new List<Claim>{
            new Claim(ClaimTypes.NameIdentifier,user.Id),
            new Claim(ClaimTypes.Email,user.Email),
            new Claim(JwtRegisteredClaimNames.Name,user.UserName)
        };

        var expires = DateTime.UtcNow.AddHours(3);

        JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
            issuer : _jwtOptions.Issuer,
            audience : _jwtOptions.Audience,
            claims : claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtOptions.SecurityKey)),
                SecurityAlgorithms.HmacSha256Signature));

        string token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

        string refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationInDays);

        IdentityResult updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new BusinessException("Could not persist refresh token");

        var response = new TokenResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            RefreshTokenExpires = Convert.ToDateTime(user.RefreshTokenExpires)
        };

        return response;
    }

    private string GenerateRefreshToken()
    {
        Guid refreshTokenGuid = Guid.NewGuid();
        return refreshTokenGuid.ToString();
    }
}