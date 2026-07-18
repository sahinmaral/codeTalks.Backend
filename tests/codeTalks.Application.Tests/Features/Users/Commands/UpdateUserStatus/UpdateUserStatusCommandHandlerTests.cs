using System.Linq.Expressions;
using codeTalks.Application.Features.Users.Commands.UpdateUserStatus;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using FluentAssertions;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Users.Commands.UpdateUserStatus;

// The handler has two branches keyed on whether a UserStatus row already exists:
//   - none  -> AddAsync a new row
//   - exists -> mutate Status and Update
// Both repository dependencies are interfaces, so they mock cleanly with NSubstitute.
public class UpdateUserStatusCommandHandlerTests
{
    private const string UserId = "user-123";

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserStatusRepository _userStatusRepository = Substitute.For<IUserStatusRepository>();
    private readonly UpdateUserStatusCommand.UpdateUserStatusCommandHandler _handler;

    public UpdateUserStatusCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(UserId);
        _handler = new UpdateUserStatusCommand.UpdateUserStatusCommandHandler(
            _currentUserService, _userStatusRepository);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoStatusYet_AddsNewStatusForCurrentUser()
    {
        // Arrange: no existing row
        _userStatusRepository
            .GetAsync(Arg.Any<Expression<Func<UserStatus, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((UserStatus?)null);

        // Act
        await _handler.Handle(new UpdateUserStatusCommand { Status = UserStatusType.Busy }, CancellationToken.None);

        // Assert: a new row is created for this user with the requested status; no update
        await _userStatusRepository.Received(1).AddAsync(
            Arg.Is<UserStatus>(s => s.UserId == UserId && s.Status == UserStatusType.Busy),
            Arg.Any<CancellationToken>());
        _userStatusRepository.DidNotReceive().Update(Arg.Any<UserStatus>());
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyHasStatus_UpdatesExistingStatus()
    {
        // Arrange: existing row currently Online
        var existing = new UserStatus { UserId = UserId, Status = UserStatusType.Online };
        _userStatusRepository
            .GetAsync(Arg.Any<Expression<Func<UserStatus, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        // Act
        await _handler.Handle(new UpdateUserStatusCommand { Status = UserStatusType.Away }, CancellationToken.None);

        // Assert: the same entity is mutated and updated; nothing new is added
        existing.Status.Should().Be(UserStatusType.Away);
        _userStatusRepository.Received(1).Update(existing);
        await _userStatusRepository.DidNotReceive().AddAsync(Arg.Any<UserStatus>(), Arg.Any<CancellationToken>());
    }
}