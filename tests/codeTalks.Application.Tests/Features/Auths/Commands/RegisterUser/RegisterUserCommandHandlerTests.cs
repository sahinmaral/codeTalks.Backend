using codeTalks.Application.Features.Auths.Commands.RegisterUser;
using codeTalks.Application.Features.Auths.Dtos;
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.Tests.TestUtilities;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Auths.Commands.RegisterUser;

// Registration: reject duplicate username/email, map+create the Identity user, then seed
// an Online status row and default notification settings, and return the mapped DTO.
public class RegisterUserCommandHandlerTests
{
    private const string UserName = "janedoe";
    private const string Email = "jane@example.com";
    private const string Password = "secret1";
    private const string NewUserId = "new-user-id";

    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly IUserStatusRepository _userStatusRepository = Substitute.For<IUserStatusRepository>();
    private readonly IUserNotificationSettingRepository _notificationSettingRepository = Substitute.For<IUserNotificationSettingRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly RegisterUserCommand.RegisterUserCommandHandler _handler;

    private readonly User _newUser = new() { Id = NewUserId, UserName = UserName, Email = Email };

    public RegisterUserCommandHandlerTests()
    {
        var authBusinessRules = new AuthBusinessRules(_userManager);
        _handler = new RegisterUserCommand.RegisterUserCommandHandler(
            _userManager, _userStatusRepository, _notificationSettingRepository, authBusinessRules, _mapper);
    }

    private static RegisterUserCommand Command() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        UserName = UserName,
        Email = Email,
        Password = Password
    };

    private void ArrangeAvailableUsernameAndEmail()
    {
        _userManager.FindByNameAsync(UserName).Returns((User?)null);
        _userManager.FindByEmailAsync(Email).Returns((User?)null);
    }

    [Fact]
    public async Task Handle_WhenUsernameAndEmailAvailable_CreatesUserAndSeedsStatusAndSettings()
    {
        ArrangeAvailableUsernameAndEmail();
        var command = Command();
        var expectedDto = new RegisteredUserDto { Id = NewUserId, UserName = UserName, Email = Email };
        _mapper.Map<User>(command).Returns(_newUser);
        _mapper.Map<RegisteredUserDto>(_newUser).Returns(expectedDto);
        _userManager.CreateAsync(_newUser, Password).Returns(IdentityResult.Success);

        var result = await _handler.Handle(command, CancellationToken.None);

        await _userManager.Received(1).CreateAsync(_newUser, Password);
        _userStatusRepository.Received(1).Add(
            Arg.Is<UserStatus>(s => s.UserId == NewUserId && s.Status == UserStatusType.Online));
        _notificationSettingRepository.Received(1).Add(
            Arg.Is<UserNotificationSetting>(s => s.UserId == NewUserId && s.IsEnabled && !s.IsSoundEnabled));
        result.Should().BeSameAs(expectedDto);
    }

    [Fact]
    public async Task Handle_WhenUserCreationFails_ThrowsBusinessAndDoesNotSeed()
    {
        ArrangeAvailableUsernameAndEmail();
        var command = Command();
        _mapper.Map<User>(command).Returns(_newUser);
        _userManager.CreateAsync(_newUser, Password)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*Password too weak*");
        _userStatusRepository.DidNotReceive().Add(Arg.Any<UserStatus>());
        _notificationSettingRepository.DidNotReceive().Add(Arg.Any<UserNotificationSetting>());
    }

    [Fact]
    public async Task Handle_WhenUsernameAlreadyTaken_ThrowsBusinessAndDoesNotCreate()
    {
        _userManager.FindByNameAsync(UserName).Returns(new User { UserName = UserName });

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*username is already taken*");
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<string>());
        _userStatusRepository.DidNotReceive().Add(Arg.Any<UserStatus>());
        _notificationSettingRepository.DidNotReceive().Add(Arg.Any<UserNotificationSetting>());
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyTaken_ThrowsBusinessAndDoesNotCreate()
    {
        _userManager.FindByNameAsync(UserName).Returns((User?)null);
        _userManager.FindByEmailAsync(Email).Returns(new User { Email = Email });

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*e-mail address is already taken*");
        await _userManager.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<string>());
        _userStatusRepository.DidNotReceive().Add(Arg.Any<UserStatus>());
    }
}