using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Commands.UpdateChannel;
using codeTalks.Application.Features.Channels.Dtos;
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

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.UpdateChannel;

// Only the channel Owner may update channel information. Name/Description are optional:
// a null value keeps the current one. Roles are per-channel (ChannelUser.Role).
public class UpdateChannelCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };
    private static readonly Role UserRole = new() { Id = "role-user", Name = "User" };

    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly RoleManager<Role> _roleManager = RoleManagerMock.Create();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly UpdateChannelCommand.UpdateChannelCommandHandler _handler;

    public UpdateChannelCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("Owner").Returns(OwnerRole);
        _handler = new UpdateChannelCommand.UpdateChannelCommandHandler(
            _channelRepository, _roleManager, _currentUserService);
    }

    private static ChannelUser Member(string userId, Role role) =>
        new() { UserId = userId, Role = role, RoleId = role.Id, Status = ChannelUserStatus.Accepted };

    private Channel SetupChannel(string name, string description, params ChannelUser[] members)
    {
        var channel = new Channel { Id = ChannelId, Name = name, Description = description, ChannelUsers = members.ToList() };
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(channel);
        return channel;
    }

    private static UpdateChannelCommand Command(string? name, string? description) =>
        new()
        {
            ChannelId = ChannelId,
            UpdateChannelDto = new UpdateChannelDto { Name = name!, Description = description! }
        };

    [Fact]
    public async Task Handle_WhenOwnerUpdatesBothFields_UpdatesAndPersists()
    {
        var channel = SetupChannel("Old Name", "Old Desc", Member(CurrentUserId, OwnerRole));

        await _handler.Handle(Command("New Name", "New Desc"), CancellationToken.None);

        channel.Name.Should().Be("New Name");
        channel.Description.Should().Be("New Desc");
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenFieldIsNull_KeepsExistingValue()
    {
        var channel = SetupChannel("Old Name", "Old Desc", Member(CurrentUserId, OwnerRole));

        await _handler.Handle(Command("New Name", description: null), CancellationToken.None);

        channel.Name.Should().Be("New Name");
        channel.Description.Should().Be("Old Desc", "a null description keeps the existing value");
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ThrowsAuthorizationAndDoesNotPersist()
    {
        var channel = SetupChannel("Old Name", "Old Desc", Member(CurrentUserId, UserRole));

        var act = () => _handler.Handle(Command("New Name", "New Desc"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*no authorization*");
        channel.Name.Should().Be("Old Name", "a rejected update must not mutate the channel");
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

        var act = () => _handler.Handle(Command("New Name", "New Desc"), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*channel doesn't exist*");
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAChannelMember_ThrowsEntityNotFound()
    {
        SetupChannel("Old Name", "Old Desc", Member("someone-else", OwnerRole));

        var act = () => _handler.Handle(Command("New Name", "New Desc"), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*hasn't registered*");
    }
}