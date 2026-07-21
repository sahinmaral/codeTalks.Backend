using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Commands.LeaveChannel;
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

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.LeaveChannel;

// LeaveChannel has no validator; all behavior lives in the handler. The owner is a
// special case: an owner may only leave when they are the last member (which soft-deletes
// the channel); otherwise they must transfer ownership first. A regular member is simply
// removed. Roles are per-channel, carried on ChannelUser.Role.
public class LeaveChannelCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";
    private const string OtherUserId = "other-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };
    private static readonly Role MemberRole = new() { Id = "role-user", Name = "User" };

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly RoleManager<Role> _roleManager = RoleManagerMock.Create();
    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly LeaveChannelCommand.LeaveChannelCommandHandler _handler;

    public LeaveChannelCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("Owner").Returns(OwnerRole);
        _handler = new LeaveChannelCommand.LeaveChannelCommandHandler(
            _currentUserService, _roleManager, _channelRepository);
    }

    private static ChannelUser Member(string userId, Role role) =>
        new() { UserId = userId, Role = role, RoleId = role.Id, Status = ChannelUserStatus.Accepted };

    private Channel SetupChannel(params ChannelUser[] members)
    {
        var channel = new Channel
        {
            Id = ChannelId,
            IsActive = true,
            ChannelUsers = members.ToList()
        };
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(channel);
        return channel;
    }

    private static LeaveChannelCommand Command() => new() { ChannelId = ChannelId };

    [Fact]
    public async Task Handle_WhenRegularMemberLeaves_RemovesMembershipAndKeepsChannelActive()
    {
        var owner = Member(OtherUserId, OwnerRole);
        var leaving = Member(CurrentUserId, MemberRole);
        var channel = SetupChannel(owner, leaving);

        await _handler.Handle(Command(), CancellationToken.None);

        channel.ChannelUsers.Should().NotContain(leaving);
        channel.ChannelUsers.Should().Contain(owner);
        channel.IsActive.Should().BeTrue();
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenOwnerIsTheLastMember_SoftDeletesChannel()
    {
        var owner = Member(CurrentUserId, OwnerRole);
        var channel = SetupChannel(owner);

        await _handler.Handle(Command(), CancellationToken.None);

        channel.IsActive.Should().BeFalse();
        channel.DeletedAt.Should().NotBeNull();
        channel.ChannelUsers.Should().BeEmpty();
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenOwnerLeavesWithOtherMembers_ThrowsAuthorizationAndDoesNotPersist()
    {
        var owner = Member(CurrentUserId, OwnerRole);
        var other = Member(OtherUserId, MemberRole);
        var channel = SetupChannel(owner, other);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*transfer your ownership*");
        channel.ChannelUsers.Should().Contain(owner, "the owner must not be removed when the leave is rejected");
        channel.IsActive.Should().BeTrue();
        await _channelRepository.DidNotReceive().UpdateAsync(Arg.Any<Channel>());
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
    public async Task Handle_WhenUserIsNotAChannelMember_ThrowsEntityNotFound()
    {
        SetupChannel(Member(OtherUserId, OwnerRole)); // current user is not in the channel

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*haven't registered*");
        await _channelRepository.DidNotReceive().UpdateAsync(Arg.Any<Channel>());
    }
}