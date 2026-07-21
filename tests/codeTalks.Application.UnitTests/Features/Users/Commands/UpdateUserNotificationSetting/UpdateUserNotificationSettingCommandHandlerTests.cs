using codeTalks.Application.Features.Users.Commands.UpdateUserNotificationSetting;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using FluentAssertions;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Users.Commands.UpdateUserNotificationSetting;

// Two branches keyed on whether a notification setting exists for the user:
//   none   -> create one (IsEnabled defaults to true), AddAsync
//   exists -> Update only the sound flag, preserving the existing IsEnabled, UpdateAsync
// Either way the read-model cache is refreshed. This command toggles *sound* only.
public class UpdateUserNotificationSettingCommandHandlerTests
{
    private const string CurrentUserId = "current-user";

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserNotificationSettingRepository _repository = Substitute.For<IUserNotificationSettingRepository>();
    private readonly IUserSettingsCache _settingsCache = Substitute.For<IUserSettingsCache>();
    private readonly UpdateUserNotificationSettingCommand.UpdateUserNotificationSettingCommandHandler _handler;

    public UpdateUserNotificationSettingCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _handler = new UpdateUserNotificationSettingCommand.UpdateUserNotificationSettingCommandHandler(
            _currentUserService, _repository, _settingsCache);
    }

    private static UpdateUserNotificationSettingCommand Command(bool isSoundEnabled) =>
        new() { IsSoundEnabled = isSoundEnabled };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenNoSettingExists_CreatesEnabledSettingWithRequestedSoundAndRefreshesCache(bool sound)
    {
        _repository.GetByUserIdAsync(CurrentUserId, Arg.Any<CancellationToken>())
            .Returns((UserNotificationSetting?)null);

        await _handler.Handle(Command(sound), CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<UserNotificationSetting>(s =>
                s.UserId == CurrentUserId &&
                s.IsEnabled &&               // new settings default to enabled
                s.IsSoundEnabled == sound),
            Arg.Any<CancellationToken>());
        await _settingsCache.Received(1).SetNotificationSettingAsync(CurrentUserId, true, sound);
    }

    [Fact]
    public async Task Handle_WhenSettingExists_UpdatesSoundOnlyAndPreservesIsEnabled()
    {
        // Existing setting is disabled; changing the sound flag must NOT re-enable notifications.
        var existing = new UserNotificationSetting
        {
            UserId = CurrentUserId,
            IsEnabled = false,
            IsSoundEnabled = false
        };
        _repository.GetByUserIdAsync(CurrentUserId, Arg.Any<CancellationToken>()).Returns(existing);

        await _handler.Handle(Command(isSoundEnabled: true), CancellationToken.None);

        existing.IsSoundEnabled.Should().BeTrue();
        existing.IsEnabled.Should().BeFalse("toggling sound must preserve the existing enabled state");
        await _repository.Received(1).UpdateAsync(existing);
        await _repository.DidNotReceive().AddAsync(Arg.Any<UserNotificationSetting>(), Arg.Any<CancellationToken>());
        await _settingsCache.Received(1).SetNotificationSettingAsync(CurrentUserId, false, true);
    }
}