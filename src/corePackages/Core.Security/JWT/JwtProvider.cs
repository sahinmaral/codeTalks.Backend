using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

        var expires = DateTime.UtcNow.AddHours(1);

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
        user.RefreshTokenExpires = expires.AddMinutes(_jwtOptions.RefreshTokenExpiration);

        await userManager.UpdateAsync(user);

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