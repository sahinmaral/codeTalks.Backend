using System.Linq.Expressions;
using CloudinaryDotNet.Actions;
using codeTalks.Application.Features.Channels.Commands.UpdateThumbnailPhoto;
using codeTalks.Application.Services;
using codeTalks.Application.Services.FileStorage;
using codeTalks.Application.Services.Repositories;
using codeTalks.Application.UnitTests.TestUtilities;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Role = Core.Security.Entities.Role;

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.UpdateThumbnailPhoto;

// Only the channel Owner may set the thumbnail. If a thumbnail already exists, the old
// Cloudinary image is deleted first; then the new image is uploaded, the channel's
// ThumbnailPhotoURL is updated, and the new URL is returned. Roles are per-channel.
public class UpdateThumbnailPhotoCommandHandlerTests
{
    private const string ChannelId = "channel-1";
    private const string CurrentUserId = "current-user";
    private const string NewPhotoUrl = "https://res.cloudinary.com/demo/image/upload/v999/channels/newthumb.png";

    private static readonly Role OwnerRole = new() { Id = "role-owner", Name = "Owner" };
    private static readonly Role UserRole = new() { Id = "role-user", Name = "User" };

    private readonly ICloudinaryService _cloudinaryService = Substitute.For<ICloudinaryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly RoleManager<Role> _roleManager = RoleManagerMock.Create();
    private readonly IChannelRepository _channelRepository = Substitute.For<IChannelRepository>();
    private readonly UpdateThumbnailPhotoCommand.UpdateThumbnailPhotoCommandHandler _handler;

    public UpdateThumbnailPhotoCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _roleManager.FindByNameAsync("Owner").Returns(OwnerRole);
        _cloudinaryService.UploadImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>())
            .Returns(new ImageUploadResult { SecureUrl = new Uri(NewPhotoUrl) });
        _handler = new UpdateThumbnailPhotoCommand.UpdateThumbnailPhotoCommandHandler(
            _cloudinaryService, _currentUserService, _roleManager, _channelRepository);
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

    private static UpdateThumbnailPhotoCommand Command() =>
        new() { ChannelId = ChannelId, Image = Substitute.For<IFormFile>() };

    [Fact]
    public async Task Handle_WhenOwnerAndNoExistingThumbnail_UploadsAndSetsUrlWithoutDeleting()
    {
        var channel = SetupChannel(thumbnailUrl: null, Member(CurrentUserId, OwnerRole));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        await _cloudinaryService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        channel.ThumbnailPhotoURL.Should().Be(NewPhotoUrl);
        result.NewThumbnailPhotoPath.Should().Be(NewPhotoUrl);
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenOwnerAndExistingThumbnail_DeletesOldByPublicIdThenUploads()
    {
        var channel = SetupChannel(
            thumbnailUrl: "https://res.cloudinary.com/demo/image/upload/v1234567890/channels/oldthumb.png",
            Member(CurrentUserId, OwnerRole));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        await _cloudinaryService.Received(1).DeleteImageAsync("channels/oldthumb", Arg.Any<CancellationToken>());
        channel.ThumbnailPhotoURL.Should().Be(NewPhotoUrl);
        result.NewThumbnailPhotoPath.Should().Be(NewPhotoUrl);
        await _channelRepository.Received(1).UpdateAsync(channel);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ThrowsAuthorizationAndDoesNotUploadOrPersist()
    {
        SetupChannel(thumbnailUrl: null, Member(CurrentUserId, UserRole));

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>().WithMessage("*no authorization*");
        await _cloudinaryService.DidNotReceive().UploadImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
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