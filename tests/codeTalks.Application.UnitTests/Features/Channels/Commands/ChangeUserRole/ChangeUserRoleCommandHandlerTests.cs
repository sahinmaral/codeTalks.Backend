using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Commands.ChangeUserRole;
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

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.ChangeUserRole;

// This handler is authorization-heavy: only a channel Owner may change roles, callers
// can't change their own role or another owner's, and promoting someone to Owner demotes
// the current owner to User. Roles are per-channel (carried on ChannelUser), so the tests
// build a channel graph with ChannelUsers that already have their Role navigation set.
public class ChangeUserRoleCommandHandlerTests
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
    private readonly ChangeUserRoleCommand.ChangeUserRoleCommandHandler _handler;

    public ChangeUserRoleCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("Owner").Returns(OwnerRole);
        _roleManager.FindByNameAsync("Moderator").Returns(ModeratorRole);
        _roleManager.FindByNameAsync("User").Returns(UserRole);

        _handler = new ChangeUserRoleCommand.ChangeUserRoleCommandHandler(
            _channelRepository, _roleManager, _userManager, _currentUserService);
    }

    private static ChannelUser Member(string userId, Role role) =>
        new() { UserId = userId, Role = role, RoleId = role.Id };

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

    private static ChangeUserRoleCommand Command(string role, string targetUserId = TargetUserId) =>
        new() { ChannelId = ChannelId, UserId = targetUserId, Role = role };

    [Fact]
    public async Task Handle_WhenOwnerPromotesMemberToModerator_UpdatesTargetRoleAndPersists()
    {
        var owner = Member(CurrentUserId, OwnerRole);
        var target = Member(TargetUserId, UserRole);
        var channel = SetupChannel(owner, target);
        SetupExistingUsers(TargetUserId);

        await _handler.Handle(Command("Moderator"), CancellationToken.None);

        target.RoleId.Should().Be(ModeratorRole.Id);
        owner.RoleId.Should().Be(OwnerRole.Id, "the owner's role is untouched for a normal promotion");
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenOwnerPromotesMemberToOwner_TransfersOwnershipAndDemotesCaller()
    {
        var owner = Member(CurrentUserId, OwnerRole);
        var target = Member(TargetUserId, UserRole);
        var channel = SetupChannel(owner, target);
        SetupExistingUsers(TargetUserId);

        await _handler.Handle(Command("Owner"), CancellationToken.None);

        target.RoleId.Should().Be(OwnerRole.Id);
        owner.RoleId.Should().Be(UserRole.Id, "promoting a new owner demotes the previous owner to User");
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ThrowsAuthorizationAndDoesNotPersist()
    {
        var caller = Member(CurrentUserId, ModeratorRole); // not an owner
        var target = Member(TargetUserId, UserRole);
        SetupChannel(caller, target);
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command("Moderator"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*owner*");
        await _channelRepository.DidNotReceive().UpdateAsync(Arg.Any<Channel>());
    }

    [Fact]
    public async Task Handle_WhenOwnerTargetsThemselves_ThrowsAuthorization()
    {
        var owner = Member(CurrentUserId, OwnerRole);
        SetupChannel(owner);
        SetupExistingUsers(CurrentUserId);

        var act = () => _handler.Handle(Command("Moderator", targetUserId: CurrentUserId), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*your own role*");
    }

    [Fact]
    public async Task Handle_WhenTargetIsAnotherOwner_ThrowsAuthorization()
    {
        var caller = Member(CurrentUserId, OwnerRole);
        var otherOwner = Member(TargetUserId, OwnerRole);
        SetupChannel(caller, otherOwner);
        SetupExistingUsers(TargetUserId);

        var act = () => _handler.Handle(Command("Moderator"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*channel owner*");
    }

    [Fact]
    public async Task Handle_WhenRequestedRoleDoesNotExist_ThrowsEntityNotFound()
    {
        _roleManager.FindByNameAsync("Ghost").Returns((Role?)null);

        var act = () => _handler.Handle(Command("Ghost"), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*role*");
    }

    [Fact]
    public async Task Handle_WhenTargetUserIsNotAChannelMember_ThrowsEntityNotFound()
    {
        var owner = Member(CurrentUserId, OwnerRole);
        SetupChannel(owner); // target is not registered in the channel
        SetupExistingUsers(TargetUserId); // but exists globally

        var act = () => _handler.Handle(Command("Moderator"), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*hasn't registered*");
    }
}