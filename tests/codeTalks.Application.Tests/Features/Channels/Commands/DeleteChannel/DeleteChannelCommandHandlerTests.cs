using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Commands.DeleteChannel;
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

namespace codeTalks.Application.Tests.Features.Channels.Commands.DeleteChannel;

// Only the channel Owner may delete a channel, and deletion is a soft-delete
// (IsActive = false + DeletedAt set). Roles are per-channel (ChannelUser.Role).
public class DeleteChannelCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };
    private static readonly Role UserRole = new() { Id = "role-user", Name = "User" };

    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly RoleManager<Role> _roleManager = RoleManagerMock.Create();
    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly DeleteChannelCommand.DeleteChannelCommandHandler _handler;

    public DeleteChannelCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("Owner").Returns(OwnerRole);
        _handler = new DeleteChannelCommand.DeleteChannelCommandHandler(
            _currentUserService, _roleManager, _channelRepository);
    }

    private static ChannelUser Member(string userId, Role role) =>
        new() { UserId = userId, Role = role, RoleId = role.Id, Status = ChannelUserStatus.Accepted };

    private Channel SetupChannel(params ChannelUser[] members)
    {
        var channel = new Channel { Id = ChannelId, IsActive = true, ChannelUsers = members.ToList() };
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(channel);
        return channel;
    }

    private static DeleteChannelCommand Command() => new() { ChannelId = ChannelId };

    [Fact]
    public async Task Handle_WhenOwnerDeletes_SoftDeletesChannelAndPersists()
    {
        var channel = SetupChannel(Member(CurrentUserId, OwnerRole));

        await _handler.Handle(Command(), CancellationToken.None);

        channel.IsActive.Should().BeFalse();
        channel.DeletedAt.Should().NotBeNull();
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ThrowsAuthorizationAndDoesNotPersist()
    {
        var channel = SetupChannel(Member(CurrentUserId, UserRole));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*no authorization*");
        channel.IsActive.Should().BeTrue("a rejected delete must not deactivate the channel");
        channel.DeletedAt.Should().BeNull();
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
    public async Task Handle_WhenCallerIsNotAChannelMember_ThrowsEntityNotFound()
    {
        SetupChannel(Member("someone-else", OwnerRole));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*hasn't registered*");
    }
}