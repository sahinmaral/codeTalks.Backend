using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Users.Commands.ChangeUserPassword;
using codeTalks.Application.Services;
using codeTalks.Application.UnitTests.TestUtilities;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Users.Commands.ChangeUserPassword;

// Password change runs three gates before persisting:
//   1. the current user must exist          (AuthBusinessRules.CheckUserExistsById)
//   2. the supplied current password matches (AuthBusinessRules.CheckIfUserEnteredCorrectPassword)
//   3. Identity accepts the new password     (UserManager.ChangePasswordAsync succeeds)
// AuthBusinessRules is concrete, so it's built over the same mocked UserManager the
// handler uses, and every gate is driven by configuring that UserManager.
public class ChangeUserPasswordCommandHandlerTests
{
    private const string CurrentPassword = "OldPassword1";
    private const string NewPassword = "NewPassword1";

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly ChangeUserPasswordCommand.ChangeUserPasswordCommandHandler _handler;
    private readonly User _user = new() { UserName = "jane" };

    public ChangeUserPasswordCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(_user.Id);
        var authBusinessRules = new AuthBusinessRules(_userManager);
        _handler = new ChangeUserPasswordCommand.ChangeUserPasswordCommandHandler(
            _currentUserService, _userManager, authBusinessRules);
    }

    private static ChangeUserPasswordCommand Command() =>
        new() { CurrentPassword = CurrentPassword, NewPassword = NewPassword };

    [Fact]
    public async Task Handle_WhenCurrentPasswordValidAndChangeSucceeds_ChangesPassword()
    {
        _userManager.FindByIdAsync(_user.Id).Returns(_user);
        _userManager.CheckPasswordAsync(_user, CurrentPassword).Returns(true);
        _userManager.ChangePasswordAsync(_user, CurrentPassword, NewPassword).Returns(IdentityResult.Success);

        await _handler.Handle(Command(), CancellationToken.None);

        await _userManager.Received(1).ChangePasswordAsync(_user, CurrentPassword, NewPassword);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsEntityNotFoundAndDoesNotChangePassword()
    {
        _userManager.FindByIdAsync(_user.Id).Returns((User?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        await _userManager.DidNotReceive()
            .ChangePasswordAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordIncorrect_ThrowsBusinessAndDoesNotChangePassword()
    {
        _userManager.FindByIdAsync(_user.Id).Returns(_user);
        _userManager.CheckPasswordAsync(_user, CurrentPassword).Returns(false);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*current password is incorrect*");
        await _userManager.DidNotReceive()
            .ChangePasswordAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenIdentityRejectsNewPassword_ThrowsBusinessWithIdentityErrors()
    {
        _userManager.FindByIdAsync(_user.Id).Returns(_user);
        _userManager.CheckPasswordAsync(_user, CurrentPassword).Returns(true);
        _userManager.ChangePasswordAsync(_user, CurrentPassword, NewPassword)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Passwords must have at least one digit." }));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        // The handler surfaces Identity's error descriptions in the exception message.
        await act.Should().ThrowAsync<BusinessException>().WithMessage("*at least one digit*");
    }
}