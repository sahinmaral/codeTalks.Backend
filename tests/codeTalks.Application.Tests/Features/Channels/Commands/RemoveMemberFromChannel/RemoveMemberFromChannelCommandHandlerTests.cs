using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Commands.RemoveMemberFromChannel;
using codeTalks.Application.Services;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.Tests.TestUtilities;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Channels.Commands.RemoveMemberFromChannel;

// Removal authorization matrix: only Owner/Moderator may remove members, nobody may remove
// themselves (kick != leave), and a Moderator may only remove regular members (not the owner
// or fellow moderators). Owner may remove anyone. Roles are per-channel (ChannelUser.Role).
public class RemoveMemberFromChannelCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";
    private const string TargetUserId = "target-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };
    private static readonly Role ModeratorRole = new() { Id = "role-mod", Name = "Moderator" };
    private static readonly Role UserRole = new() { Id = "role-user", Name = "User" };

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly RoleManager<Role> _roleManager = RoleManagerMock.Create();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly RemoveMemberFromChannelCommand.RemoveMemberFromChannelCommandHandler _handler;

    public RemoveMemberFromChannelCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("Moderator").Returns(ModeratorRole);
        _roleManager.FindByNameAsync("Owner").Returns(OwnerRole);
        _handler = new RemoveMemberFromChannelCommand.RemoveMemberFromChannelCommandHandler(
            _currentUserService, _roleManager, _userManager, _channelRepository);
    }

    private static Role RoleByName(string name) => name switch
    {
        "Owner" => OwnerRole,
        "Moderator" => ModeratorRole,
        _ => UserRole
    };

    private static ChannelUser Member(string userId, Role role) =>
        new() { UserId = userId, Role = role, RoleId = role.Id, Status = ChannelUserStatus.Accepted };

    private Channel SetupChannel(params ChannelUser[] members)
    {
        var channel = new Channel { Id = ChannelId, ChannelUsers = members.ToList() };
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

    private static RemoveMemberFromChannelCommand Command(string targetUserId = TargetUserId) =>
        new() { ChannelId = ChannelId, UserId = targetUserId };

    [Theory]
    [InlineData("Owner", "User")]        // owner removes a regular member
    [InlineData("Owner", "Moderator")]   // owner removes a moderator
    [InlineData("Moderator", "User")]    // moderator removes a regular member
    public async Task Handle_WhenCallerIsAuthorized_RemovesTargetAndPersists(string callerRole, string targetRole)
    {
        var caller = Member(CurrentUserId, RoleByName(callerRole));
        var target = Member(TargetUserId, RoleByName(targetRole));
        var channel = SetupChannel(caller, target);
        SetupExistingUsers(TargetUserId);

        await _handler.Handle(Command(), CancellationToken.None);

        channel.ChannelUsers.Should().NotContain(target);
        channel.ChannelUsers.Should().Contain(caller);
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Theory]
    [InlineData("Owner")]      // moderator can't remove the owner
    [InlineData("Moderator")]  // moderator can't remove another moderator
    public async Task Handle_WhenModeratorTargetsPrivilegedMember_ThrowsAuthorization(string targetRole)
    {
        var moderator = Member(CurrentUserId, ModeratorRole);
        var target = Member(TargetUserId, RoleByName(targetRole));
        SetupChannel(moderator, target);
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*only remove regular members*");
        await _channelRepository.DidNotReceive().UpdateAsync(Arg.Any<Channel>());
    }

    [Fact]
    public async Task Handle_WhenCallerIsRegularMember_ThrowsAuthorization()
    {
        var caller = Member(CurrentUserId, UserRole);
        var target = Member(TargetUserId, UserRole);
        SetupChannel(caller, target);
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*no authorization*");
        await _channelRepository.DidNotReceive().UpdateAsync(Arg.Any<Channel>());
    }

    [Fact]
    public async Task Handle_WhenCallerTargetsThemselves_ThrowsAuthorization()
    {
        // Self-removal is rejected before the role check: kicking yourself isn't allowed (use LeaveChannel).
        var owner = Member(CurrentUserId, OwnerRole);
        SetupChannel(owner);
        SetupExistingUsers(CurrentUserId);

        var act = () => _handler.Handle(Command(targetUserId: CurrentUserId), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*remove yourself*");
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

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*channel doesn't exist*");
    }

    [Fact]
    public async Task Handle_WhenTargetUserDoesNotExist_ThrowsEntityNotFound()
    {
        SetupChannel(Member(CurrentUserId, OwnerRole));
        SetupExistingUsers(); // no users

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*user doesn't exist*");
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAChannelMember_ThrowsEntityNotFound()
    {
        SetupChannel(Member(TargetUserId, UserRole)); // caller is not in the channel
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*You haven't registered*");
    }

    [Fact]
    public async Task Handle_WhenTargetIsNotAChannelMember_ThrowsEntityNotFound()
    {
        SetupChannel(Member(CurrentUserId, OwnerRole)); // target exists globally but not in channel
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*hasn't registered*");
    }
}