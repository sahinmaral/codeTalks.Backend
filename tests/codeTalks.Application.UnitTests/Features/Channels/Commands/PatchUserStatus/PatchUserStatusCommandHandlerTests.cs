using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Commands.PatchUserStatus;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.UnitTests.TestUtilities;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.PatchUserStatus;

// PatchUserStatus changes another member's channel status (accept/deny/ban/unban) and is
// the most authorization-heavy handler: only Owner/Moderator may act, nobody may patch
// their own status, and ban/unban of an owner or fellow moderator is reserved to the owner.
// Roles are per-channel (carried on ChannelUser.Role).
public class PatchUserStatusCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";
    private const string TargetUserId = "target-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };
    private static readonly Role ModeratorRole = new() { Id = "role-mod", Name = "Moderator" };
    private static readonly Role UserRole = new() { Id = "role-user", Name = "User" };

    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly RoleManager<Role> _roleManager = RoleManagerMock.Create();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly PatchUserStatusCommand.PatchUserStatusCommandHandler _handler;

    public PatchUserStatusCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("Moderator").Returns(ModeratorRole);
        _roleManager.FindByNameAsync("Owner").Returns(OwnerRole);
        _handler = new PatchUserStatusCommand.PatchUserStatusCommandHandler(
            _channelRepository, _roleManager, _userManager, _currentUserService);
    }

    private static ChannelUser Member(string userId, Role role, ChannelUserStatus status = ChannelUserStatus.Accepted) =>
        new() { UserId = userId, Role = role, RoleId = role.Id, Status = status };

    private Channel SetupChannel(ChannelJoinPolicy joinPolicy, params ChannelUser[] members)
    {
        var channel = new Channel { Id = ChannelId, JoinPolicy = joinPolicy, ChannelUsers = members.ToList() };
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(channel);
        return channel;
    }

    private void SetupExistingUsers(params string[] userIds) =>
        _userManager.Users.Returns(TestAsyncQueryable.From(userIds.Select(id => new User { Id = id })));

    private static PatchUserStatusCommand Command(ChannelUserStatus status, string targetUserId = TargetUserId) =>
        new() { ChannelId = ChannelId, UserId = targetUserId, Status = status };

    [Fact]
    public async Task Handle_WhenModeratorAcceptsPendingRequest_UpdatesStatusAndPersists()
    {
        var moderator = Member(CurrentUserId, ModeratorRole);
        var pending = Member(TargetUserId, UserRole, ChannelUserStatus.RequestSent);
        var channel = SetupChannel(ChannelJoinPolicy.Request, moderator, pending);
        SetupExistingUsers(TargetUserId);

        await _handler.Handle(Command(ChannelUserStatus.Accepted), CancellationToken.None);

        pending.Status.Should().Be(ChannelUserStatus.Accepted);
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenOwnerBansMember_UpdatesStatusToBanned()
    {
        var owner = Member(CurrentUserId, OwnerRole);
        var target = Member(TargetUserId, UserRole);
        var channel = SetupChannel(ChannelJoinPolicy.Request, owner, target);
        SetupExistingUsers(TargetUserId);

        await _handler.Handle(Command(ChannelUserStatus.Banned), CancellationToken.None);

        target.Status.Should().Be(ChannelUserStatus.Banned);
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenModeratorTriesToBanAnotherModerator_ThrowsAuthorization()
    {
        var moderator = Member(CurrentUserId, ModeratorRole);
        var otherModerator = Member(TargetUserId, ModeratorRole);
        SetupChannel(ChannelJoinPolicy.Request, moderator, otherModerator);
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command(ChannelUserStatus.Banned), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*You can't ban another moderator*");
        await _channelRepository.DidNotReceive().UpdateAsync(Arg.Any<Channel>());
    }

    [Fact]
    public async Task Handle_WhenModeratorTriesToBanOwner_ThrowsAuthorization()
    {
        var moderator = Member(CurrentUserId, ModeratorRole);
        var owner = Member(TargetUserId, OwnerRole);
        SetupChannel(ChannelJoinPolicy.Request, moderator, owner);
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command(ChannelUserStatus.Banned), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*ban owner*");
    }

    [Fact]
    public async Task Handle_WhenModeratorTriesToUnbanAnotherModerator_ThrowsAuthorization()
    {
        // Unban = setting Accepted on a currently-Banned member; the same owner-only rule applies.
        var moderator = Member(CurrentUserId, ModeratorRole);
        var bannedModerator = Member(TargetUserId, ModeratorRole, ChannelUserStatus.Banned);
        SetupChannel(ChannelJoinPolicy.Request, moderator, bannedModerator);
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command(ChannelUserStatus.Accepted), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*You can't unban another moderator*");
    }

    [Fact]
    public async Task Handle_WhenCallerIsRegularMember_ThrowsAuthorization()
    {
        var caller = Member(CurrentUserId, UserRole);
        var target = Member(TargetUserId, UserRole, ChannelUserStatus.RequestSent);
        SetupChannel(ChannelJoinPolicy.Request, caller, target);
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command(ChannelUserStatus.Accepted), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*no authorization*");
    }

    [Fact]
    public async Task Handle_WhenCallerPatchesOwnStatus_ThrowsAuthorization()
    {
        var moderator = Member(CurrentUserId, ModeratorRole);
        SetupChannel(ChannelJoinPolicy.Request, moderator);
        SetupExistingUsers(CurrentUserId);

        var act = () => _handler.Handle(Command(ChannelUserStatus.Accepted, targetUserId: CurrentUserId), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*your own channel status*");
    }

    [Fact]
    public async Task Handle_WhenChannelIsOpenAndStatusAccepted_ThrowsBusiness()
    {
        SetupChannel(ChannelJoinPolicy.Open);

        var act = () => _handler.Handle(Command(ChannelUserStatus.Accepted), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*channel status to Accepted*");
    }

    [Fact]
    public async Task Handle_WhenModeratorDeniesPendingRequestInRequestChannel_UpdatesStatusAndPersists()
    {
        // The early guard only blocks Accepted/Denied in an Open channel. In a Request channel a
        // moderator may deny a pending join request. (This exercises the fixed operator precedence:
        // `Open && (Accepted || Denied)` rather than `(Open && Accepted) || Denied`.)
        var moderator = Member(CurrentUserId, ModeratorRole);
        var pending = Member(TargetUserId, UserRole, ChannelUserStatus.RequestSent);
        var channel = SetupChannel(ChannelJoinPolicy.Request, moderator, pending);
        SetupExistingUsers(TargetUserId);

        await _handler.Handle(Command(ChannelUserStatus.Denied), CancellationToken.None);

        pending.Status.Should().Be(ChannelUserStatus.Denied);
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenChannelIsOpenAndStatusDenied_ThrowsBusiness()
    {
        SetupChannel(ChannelJoinPolicy.Open);

        var act = () => _handler.Handle(Command(ChannelUserStatus.Denied), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*channel status to Denied*");
    }

    [Fact]
    public async Task Handle_WhenChannelDoesNotExist_ThrowsEntityNotFound()
    {
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns((Channel?)null);

        var act = () => _handler.Handle(Command(ChannelUserStatus.Accepted), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*channel doesn't exist*");
    }

    [Fact]
    public async Task Handle_WhenTargetUserDoesNotExist_ThrowsEntityNotFound()
    {
        SetupChannel(ChannelJoinPolicy.Request, Member(CurrentUserId, ModeratorRole));
        SetupExistingUsers(); // no users at all

        var act = () => _handler.Handle(Command(ChannelUserStatus.Accepted), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*user doesn't exist*");
    }

    [Fact]
    public async Task Handle_WhenTargetIsNotAChannelMember_ThrowsEntityNotFound()
    {
        SetupChannel(ChannelJoinPolicy.Request, Member(CurrentUserId, ModeratorRole));
        SetupExistingUsers(TargetUserId); // exists globally, but not in the channel

        var act = () => _handler.Handle(Command(ChannelUserStatus.Accepted), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*hasn't registered*");
    }
}