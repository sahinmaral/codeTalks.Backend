using codeTalks.Application.Features.Auths.Commands.RefreshToken;
using codeTalks.Application.Tests.TestUtilities;
using Core.Security.Entities;
using Core.Security.JWT;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Auths.Commands.RefreshToken;

// Refresh: find the user whose stored refresh token matches, reject if missing or expired,
// otherwise reissue a fresh token pair via IJwtProvider. The lookup queries UserManager.Users
// (async), so TestAsyncQueryable is used.
public class RefreshTokenCommandHandlerTests
{
    private const string RefreshToken = "valid-refresh-token";

    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
    private readonly RefreshTokenCommand.RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommand.RefreshTokenCommandHandler(_userManager, _jwtProvider);
    }

    private void SetupExistingUsers(params User[] users) =>
        _userManager.Users.Returns(TestAsyncQueryable.From(users));

    private static RefreshTokenCommand Command() => new() { RefreshToken = RefreshToken };

    [Fact]
    public async Task Handle_WhenRefreshTokenValidAndNotExpired_ReissuesTokenPair()
    {
        var user = new User { Id = "u1", RefreshToken = RefreshToken, RefreshTokenExpires = DateTime.UtcNow.AddDays(1) };
        SetupExistingUsers(user);
        var tokens = new TokenResponse
        {
            AccessToken = "new-access",
            RefreshToken = "new-refresh",
            RefreshTokenExpires = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        _jwtProvider.CreateTokenAsync(user).Returns(tokens);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        await _jwtProvider.Received(1).CreateTokenAsync(user);
        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
        result.RefreshTokenExpires.Should().Be(tokens.RefreshTokenExpires);
    }

    [Fact]
    public async Task Handle_WhenNoUserHasThatRefreshToken_ThrowsSecurityTokenInvalid()
    {
        SetupExistingUsers(); // no user matches the token

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<SecurityTokenException>().WithMessage("*invalid*");
        await _jwtProvider.DidNotReceive().CreateTokenAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenExpired_ThrowsSecurityTokenExpiredAndDoesNotReissue()
    {
        var user = new User { Id = "u1", RefreshToken = RefreshToken, RefreshTokenExpires = DateTime.UtcNow.AddDays(-1) };
        SetupExistingUsers(user);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<SecurityTokenException>().WithMessage("*expired*");
        await _jwtProvider.DidNotReceive().CreateTokenAsync(Arg.Any<User>());
    }
}