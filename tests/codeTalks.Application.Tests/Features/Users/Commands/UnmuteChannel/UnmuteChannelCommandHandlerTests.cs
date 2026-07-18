using codeTalks.Application.Features.Users.Commands.UnmuteChannel;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Notifications.Interfaces;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Users.Commands.UnmuteChannel;

// Unmute deletes the (user, channel) mute setting and invalidates the cached read-model.
// If no setting exists there is nothing to unmute, which is a business error.
public class UnmuteChannelCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUserChannelMuteSettingRepository _repository = Substitute.For<IUserChannelMuteSettingRepository>();
    private readonly IUserSettingsCache _settingsCache = Substitute.For<IUserSettingsCache>();
    private readonly UnmuteChannelCommand.UnmuteChannelCommandHandler _handler;

    public UnmuteChannelCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _handler = new UnmuteChannelCommand.UnmuteChannelCommandHandler(
            _currentUserService, _repository, _settingsCache);
    }

    private static UnmuteChannelCommand Command() => new() { ChannelId = ChannelId };

    [Fact]
    public async Task Handle_WhenMuteSettingExists_DeletesItAndInvalidatesCache()
    {
        var existing = new UserChannelMuteSetting { UserId = CurrentUserId, ChannelId = ChannelId };
        _repository.GetAsync(CurrentUserId, ChannelId, Arg.Any<CancellationToken>()).Returns(existing);

        await _handler.Handle(Command(), CancellationToken.None);

        await _repository.Received(1).DeleteAsync(existing, Arg.Any<CancellationToken>());
        await _settingsCache.Received(1).InvalidateMuteSettingAsync(CurrentUserId, ChannelId);
    }

    [Fact]
    public async Task Handle_WhenNoMuteSettingExists_ThrowsBusinessAndDoesNothing()
    {
        _repository.GetAsync(CurrentUserId, ChannelId, Arg.Any<CancellationToken>())
            .Returns((UserChannelMuteSetting?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*haven't set this channel as muted*");
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<UserChannelMuteSetting>(), Arg.Any<CancellationToken>());
        await _settingsCache.DidNotReceive().InvalidateMuteSettingAsync(Arg.Any<string>(), Arg.Any<string>());
    }
}