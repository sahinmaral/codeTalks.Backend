using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Commands.JoinChannel;
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

namespace codeTalks.Application.Tests.Features.Channels.Commands.JoinChannel;

// Joining branches two ways:
//   1. existing membership status can block the join (Banned / Accepted / RequestSent)
//   2. otherwise the channel's JoinPolicy decides the new status:
//      Open   -> Accepted immediately,  Request -> RequestSent (pending approval)
public class JoinChannelCommandHandlerTests
{
    private const string InviteCode = "invite-1";
    private const string CurrentUserId = "current-user";

    private static readonly Role UserRole = new() { Id = "role-user", Name = "User" };

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly RoleManager<Role> _roleManager = RoleManagerMock.Create();
    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly JoinChannelCommand.JoinChannelCommandHandler _handler;

    public JoinChannelCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("User").Returns(UserRole);
        _handler = new JoinChannelCommand.JoinChannelCommandHandler(
            _currentUserService, _roleManager, _channelRepository);
    }

    private Channel SetupChannel(ChannelJoinPolicy joinPolicy, params ChannelUser[] members)
    {
        var channel = new Channel
        {
            InviteCode = InviteCode,
            JoinPolicy = joinPolicy,
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

    private static ChannelUser ExistingMember(ChannelUserStatus status) =>
        new() { UserId = CurrentUserId, Status = status };

    private static JoinChannelCommand Command() => new() { InviteCode = InviteCode };

    [Fact]
    public async Task Handle_WhenChannelIsOpen_AddsAcceptedMemberAndReturnsAccepted()
    {
        var channel = SetupChannel(ChannelJoinPolicy.Open);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Status.Should().Be(ChannelUserStatus.Accepted);
        channel.ChannelUsers.Should().ContainSingle(cu =>
            cu.UserId == CurrentUserId
            && cu.Status == ChannelUserStatus.Accepted
            && cu.RoleId == UserRole.Id);
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenChannelRequiresApproval_AddsPendingMemberAndReturnsRequestSent()
    {
        var channel = SetupChannel(ChannelJoinPolicy.Request);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Status.Should().Be(ChannelUserStatus.RequestSent);
        channel.ChannelUsers.Should().ContainSingle(cu => cu.Status == ChannelUserStatus.RequestSent);
        await _channelRepository.Received(1).UpdateAsync(channel);
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

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Theory]
    [InlineData(ChannelUserStatus.Banned, "banned")]
    [InlineData(ChannelUserStatus.Accepted, "already accepted")]
    [InlineData(ChannelUserStatus.RequestSent, "already sent")]
    public async Task Handle_WhenMembershipStatusBlocksRejoin_ThrowsBusinessAndDoesNotPersist(
        ChannelUserStatus existingStatus, string expectedMessageFragment)
    {
        SetupChannel(ChannelJoinPolicy.Open, ExistingMember(existingStatus));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage($"*{expectedMessageFragment}*");
        await _channelRepository.DidNotReceive().UpdateAsync(Arg.Any<Channel>());
    }

    [Fact]
    public async Task Handle_WhenUserWasPreviouslyDenied_AllowsRejoin()
    {
        // Denied is not one of the blocking statuses, so a denied user may request again.
        var channel = SetupChannel(ChannelJoinPolicy.Request, ExistingMember(ChannelUserStatus.Denied));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Status.Should().Be(ChannelUserStatus.RequestSent);
        channel.ChannelUsers.Should().HaveCount(2, "a new membership row is appended rather than reusing the denied one");
        await _channelRepository.Received(1).UpdateAsync(channel);
    }
}