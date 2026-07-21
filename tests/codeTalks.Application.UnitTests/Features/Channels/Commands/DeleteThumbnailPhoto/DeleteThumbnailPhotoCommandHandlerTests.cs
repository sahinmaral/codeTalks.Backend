using System.Linq.Expressions;
using codeTalks.Application.Features.Channels.Commands.DeleteThumbnailPhoto;
using codeTalks.Application.Services;
using codeTalks.Application.Services.FileStorage;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.UnitTests.TestUtilities;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.DeleteThumbnailPhoto;

// Only the channel Owner may delete the thumbnail, and only if one exists. When present,
// the Cloudinary image is removed (by public id derived from its URL) and the channel's
// ThumbnailPhotoURL is cleared. Roles are per-channel (ChannelUser.Role).
public class DeleteThumbnailPhotoCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };
    private static readonly Role UserRole = new() { Id = "role-user", Name = "User" };

    private readonly ICloudinaryService _cloudinaryService = Substitute.For<ICloudinaryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly RoleManager<Role> _roleManager = RoleManagerMock.Create();
    private readonly DeleteThumbnailPhotoCommand.DeleteThumbnailPhotoCommandHandler _handler;

    public DeleteThumbnailPhotoCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("Owner").Returns(OwnerRole);
        _handler = new DeleteThumbnailPhotoCommand.DeleteThumbnailPhotoCommandHandler(
            _cloudinaryService, _currentUserService, _channelRepository, _roleManager);
    }

    private static ChannelUser Member(string userId, Role role) =>
        new() { UserId = userId, Role = role, RoleId = role.Id, Status = ChannelUserStatus.Accepted };

    private Channel SetupChannel(string? thumbnailUrl, params ChannelUser[] members)
    {
        var channel = new Channel { Id = ChannelId, ThumbnailPhotoURL = thumbnailUrl, ChannelUsers = members.ToList() };
        _channelRepository.GetDetailedAsync(
                Arg.Any<Expression<Func<Channel, bool>>>(),
                Arg.Any<Func<IQueryable<Channel>, IIncludableQueryable<Channel, object>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(channel);
        return channel;
    }

    private static DeleteThumbnailPhotoCommand Command() => new() { ChannelId = ChannelId };

    [Fact]
    public async Task Handle_WhenOwnerAndThumbnailExists_DeletesImageByPublicIdAndClearsUrl()
    {
        var channel = SetupChannel(
            thumbnailUrl: "https://res.cloudinary.com/demo/image/upload/v1234567890/channels/oldthumb.png",
            Member(CurrentUserId, OwnerRole));

        await _handler.Handle(Command(), CancellationToken.None);

        await _cloudinaryService.Received(1).DeleteImageAsync("channels/oldthumb", Arg.Any<CancellationToken>());
        channel.ThumbnailPhotoURL.Should().BeNull();
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenOwnerButNoThumbnail_ThrowsBusinessAndDoesNotDeleteOrPersist()
    {
        SetupChannel(thumbnailUrl: null, Member(CurrentUserId, OwnerRole));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*haven't uploaded any thumbnail photo*");
        await _cloudinaryService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _channelRepository.DidNotReceive().UpdateAsync(Arg.Any<Channel>());
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ThrowsAuthorizationAndDoesNotDeleteOrPersist()
    {
        SetupChannel(
            thumbnailUrl: "https://res.cloudinary.com/demo/image/upload/v1/channels/oldthumb.png",
            Member(CurrentUserId, UserRole));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*no authorization*");
        await _cloudinaryService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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
        SetupChannel(thumbnailUrl: null, Member("someone-else", OwnerRole));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>().WithMessage("*haven't registered*");
    }
}