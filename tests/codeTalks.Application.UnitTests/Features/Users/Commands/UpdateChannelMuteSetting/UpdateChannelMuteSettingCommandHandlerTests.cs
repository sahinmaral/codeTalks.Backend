using codeTalks.Application.Features.Users.Commands.UpdateChannelMuteSetting;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using FluentAssertions;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Users.Commands.UpdateChannelMuteSetting;

// Two branches keyed on whether a mute setting already exists for (user, channel):
//   none   -> create a new setting, Mute(until), AddAsync
//   exists -> Mute(until) the existing one, UpdateAsync
// Either way the read-model cache is refreshed with the resulting IsMuted / MutedUntil.
// UserChannelMuteSetting is a rich entity: IsMuted is derived from MutedUntil vs. now.
public class UpdateChannelMuteSettingCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserChannelMuteSettingRepository _repository = Substitute.For<IUserChannelMuteSettingRepository>();
    private readonly IUserSettingsCache _settingsCache = Substitute.For<IUserSettingsCache>();
    private readonly UpdateChannelMuteSettingCommand.UpdateChannelMuteSettingCommandHandler _handler;

    public UpdateChannelMuteSettingCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _handler = new UpdateChannelMuteSettingCommand.UpdateChannelMuteSettingCommandHandler(
            _currentUserService, _repository, _settingsCache);
    }

    private static UpdateChannelMuteSettingCommand Command(DateTime muteUntil) =>
        new() { ChannelId = ChannelId, MuteUntil = muteUntil };

    [Fact]
    public async Task Handle_WhenNoSettingExists_CreatesMutedSettingAndRefreshesCache()
    {
        var muteUntil = DateTime.UtcNow.AddHours(2); // future -> IsMuted == true
        _repository.GetAsync(CurrentUserId, ChannelId, Arg.Any<CancellationToken>())
            .Returns((UserChannelMuteSetting?)null);

        await _handler.Handle(Command(muteUntil), CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<UserChannelMuteSetting>(s =>
                s.UserId == CurrentUserId &&
                s.ChannelId == ChannelId &&
                s.MutedUntil == muteUntil),
            Arg.Any<CancellationToken>());
        _repository.DidNotReceive().Update(Arg.Any<UserChannelMuteSetting>());
        await _settingsCache.Received(1).SetChannelMuteSettingAsync(CurrentUserId, ChannelId, true, muteUntil);
    }

    [Fact]
    public async Task Handle_WhenSettingExists_UpdatesExistingSettingAndRefreshesCache()
    {
        var existing = new UserChannelMuteSetting { UserId = CurrentUserId, ChannelId = ChannelId, MutedUntil = null };
        var muteUntil = DateTime.UtcNow.AddDays(1);
        _repository.GetAsync(CurrentUserId, ChannelId, Arg.Any<CancellationToken>())
            .Returns(existing);

        await _handler.Handle(Command(muteUntil), CancellationToken.None);

        existing.MutedUntil.Should().Be(muteUntil);
        await _repository.Received(1).UpdateAsync(existing);
        await _repository.DidNotReceive().AddAsync(Arg.Any<UserChannelMuteSetting>(), Arg.Any<CancellationToken>());
        await _settingsCache.Received(1).SetChannelMuteSettingAsync(CurrentUserId, ChannelId, true, muteUntil);
    }

    [Fact]
    public async Task Handle_WhenMuteUntilIsInThePast_CachesAsNotMuted()
    {
        // Domain rule: IsMuted is false once MutedUntil is in the past, so muting "until a past time"
        // effectively leaves the channel un-muted. The cache must reflect that. (The validator now
        // rejects past times at the pipeline; this test drives the handler directly to pin the domain rule.)
        var pastTime = DateTime.UtcNow.AddHours(-1);
        _repository.GetAsync(CurrentUserId, ChannelId, Arg.Any<CancellationToken>())
            .Returns((UserChannelMuteSetting?)null);

        await _handler.Handle(Command(pastTime), CancellationToken.None);

        await _settingsCache.Received(1).SetChannelMuteSettingAsync(CurrentUserId, ChannelId, false, pastTime);
    }
}