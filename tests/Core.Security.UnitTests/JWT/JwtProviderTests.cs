using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Core.Security.JWT;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Core.Security.UnitTests.JWT;

// JwtProvider builds a signed JWT with the user's identity claims, rotates the refresh token,
// and persists it via UserManager.UpdateAsync. Persistence failure must abort with a
// BusinessException so callers never receive a refresh token that wasn't saved.
public class JwtProviderTests
{
    private readonly UserManager<User> _userManager = CreateUserManager();
    private readonly JwtProvider _jwtProvider;

    private readonly JwtOptions _options = new()
    {
        Issuer = "codeTalks",
        Audience = "codeTalks-clients",
        SecurityKey = "super-secret-signing-key-that-is-at-least-32-bytes-long!!",
        RefreshTokenExpirationInDays = 7
    };

    public JwtProviderTests()
    {
        _jwtProvider = new JwtProvider(Options.Create(_options), _userManager);
    }

    private static UserManager<User> CreateUserManager()
    {
        var store = Substitute.For<IUserStore<User>>();
        return Substitute.For<UserManager<User>>(store, null, null, null, null, null, null, null, null);
    }

    private static User AUser() =>
        new() { Id = "user-1", UserName = "jane", Email = "jane@example.com" };

    [Fact]
    public async Task CreateTokenAsync_WhenPersistenceSucceeds_ReturnsSignedTokenWithUserClaims()
    {
        var user = AUser();
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var response = await _jwtProvider.CreateTokenAsync(user);

        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        jwt.Issuer.Should().Be("codeTalks");
        jwt.Audiences.Should().Contain("codeTalks-clients");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    }

    [Fact]
    public async Task CreateTokenAsync_WhenPersistenceSucceeds_RotatesAndPersistsRefreshToken()
    {
        var user = AUser();
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        var response = await _jwtProvider.CreateTokenAsync(user);

        Guid.TryParse(response.RefreshToken, out _).Should().BeTrue("the refresh token is a GUID");
        user.RefreshToken.Should().Be(response.RefreshToken, "the returned token must match what was stored");
        response.RefreshTokenExpires.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        await _userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task CreateTokenAsync_WhenPersistenceFails_ThrowsBusinessException()
    {
        var user = AUser();
        _userManager.UpdateAsync(user)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "db unavailable" }));

        var act = () => _jwtProvider.CreateTokenAsync(user);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*Could not persist refresh token*");
    }
}