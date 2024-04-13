using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Security.Entities;
using Core.Security.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Core.Security.JWT;

public class JwtProvider : IJwtProvider
{
    private readonly JwtOptions _jwtOptions;
    private readonly UserManager<User> _userManager;

    public JwtProvider(IOptions<JwtOptions> jwtOptions, UserManager<User> userManager)
    {
        _jwtOptions = jwtOptions.Value;
        _userManager = userManager;
    }

    public async Task<TokenResponse> CreateTokenAsync(User user)
    {
        var claims = new List<Claim>{
            new Claim(ClaimTypes.NameIdentifier,user.Id),
            new Claim(ClaimTypes.Email,user.Email),
            new Claim(JwtRegisteredClaimNames.Name,user.UserName)
        };

        var roles = (await _userManager.GetRolesAsync(user)).ToList();

        claims.AddRoles(roles.ToArray());

        var expires = DateTime.Now.AddHours(1);

        JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
            issuer : _jwtOptions.Issuer,
            audience : _jwtOptions.Audience,
            claims : claims,
            notBefore: DateTime.Now,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtOptions.SecurityKey)),
                SecurityAlgorithms.HmacSha256Signature));

        string token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

        string refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpires = expires.AddMinutes(_jwtOptions.RefreshTokenExpiration);

        await _userManager.UpdateAsync(user);

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