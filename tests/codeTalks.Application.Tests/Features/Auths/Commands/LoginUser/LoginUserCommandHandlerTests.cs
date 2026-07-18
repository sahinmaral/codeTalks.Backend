using codeTalks.Application.Features.Auths.Commands.LoginUser;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Tests.TestUtilities;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Core.Security.JWT;
using FluentAssertions;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Auths.Commands.LoginUser;

// Login: look up the user by username-or-email, verify the password, then issue a JWT and
// return the mapped DTO with the access/refresh tokens attached. The lookup queries
// UserManager.Users (async), so TestAsyncQueryable is used.
public class LoginUserCommandHandlerTests
{
    private const string UsernameOrEmail = "janedoe";
    private const string Password = "secret1";

    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly LoginUserCommand.LoginUserCommandHandler _handler;

    private readonly User _user = new() { Id = "u1", UserName = UsernameOrEmail, Email = "jane@example.com" };

    public LoginUserCommandHandlerTests()
    {
        var authBusinessRules = new AuthBusinessRules(_userManager);
        _handler = new LoginUserCommand.LoginUserCommandHandler(authBusinessRules, _jwtProvider, _mapper);
    }

    private void SetupExistingUsers(params User[] users) =>
        _userManager.Users.Returns(TestAsyncQueryable.From(users));

    private static LoginUserCommand Command() => new() { UsernameOrEmail = UsernameOrEmail, Password = Password };

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsMappedDtoWithIssuedTokens()
    {
        SetupExistingUsers(_user);
        _userManager.CheckPasswordAsync(_user, Password).Returns(true);
        var tokens = new TokenResponse
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            RefreshTokenExpires = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        _jwtProvider.CreateTokenAsync(_user).Returns(tokens);
        _mapper.Map<LoggedUserDto>(_user).Returns(new LoggedUserDto { Id = "u1", UserName = UsernameOrEmail });

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.RefreshTokenExpires.Should().Be(tokens.RefreshTokenExpires);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsEntityNotFoundAndDoesNotIssueToken()
    {
        SetupExistingUsers(); // no users match

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        await _jwtProvider.DidNotReceive().CreateTokenAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ThrowsBusinessAndDoesNotIssueToken()
    {
        SetupExistingUsers(_user);
        _userManager.CheckPasswordAsync(_user, Password).Returns(false);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*Invalid username/email or password*");
        await _jwtProvider.DidNotReceive().CreateTokenAsync(Arg.Any<User>());
    }
}