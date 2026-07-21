using CloudinaryDotNet.Actions;
using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Users.Commands.UpdateProfilePhoto;
using codeTalks.Application.Services;
using codeTalks.Application.Services.FileStorage;
using codeTalks.Application.UnitTests.TestUtilities;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.UnitTests.Features.Users.Commands.UpdateProfilePhoto;

// Uploads a new profile photo: if the user already has one, the old Cloudinary image is
// deleted first (by public id derived from its URL); then the new image is uploaded, the
// user's ProfilePhotoURL is updated, and the new URL is returned.
public class UpdateProfilePhotoCommandHandlerTests
{
    private const string CurrentUserId = "current-user";
    private const string NewPhotoUrl = "https://res.cloudinary.com/demo/image/upload/v999/profile/newphoto.png";

    private readonly ICloudinaryService _cloudinaryService = Substitute.For<ICloudinaryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly UpdateProfilePhotoCommand.UpdateProfilePhotoCommandHandler _handler;

    public UpdateProfilePhotoCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        _cloudinaryService.UploadImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>())
            .Returns(new ImageUploadResult { SecureUrl = new Uri(NewPhotoUrl) });

        var authBusinessRules = new AuthBusinessRules(_userManager);
        _handler = new UpdateProfilePhotoCommand.UpdateProfilePhotoCommandHandler(
            _cloudinaryService, _currentUserService, _userManager, authBusinessRules);
    }

    private static UpdateProfilePhotoCommand Command() =>
        new() { Image = Substitute.For<IFormFile>() };

    [Fact]
    public async Task Handle_WhenUserHasNoExistingPhoto_UploadsAndSetsUrlWithoutDeleting()
    {
        var user = new User { Id = CurrentUserId, ProfilePhotoURL = null };
        _userManager.FindByIdAsync(CurrentUserId).Returns(user);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        await _cloudinaryService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        user.ProfilePhotoURL.Should().Be(NewPhotoUrl);
        result.NewProfilePhotoPath.Should().Be(NewPhotoUrl);
        await _userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task Handle_WhenUserHasExistingPhoto_DeletesOldImageByPublicIdThenUploads()
    {
        var user = new User
        {
            Id = CurrentUserId,
            ProfilePhotoURL = "https://res.cloudinary.com/demo/image/upload/v1234567890/profile/oldphoto.png"
        };
        _userManager.FindByIdAsync(CurrentUserId).Returns(user);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        // version segment stripped, "/upload/" prefix removed, extension dropped
        await _cloudinaryService.Received(1).DeleteImageAsync("profile/oldphoto", Arg.Any<CancellationToken>());
        user.ProfilePhotoURL.Should().Be(NewPhotoUrl);
        result.NewProfilePhotoPath.Should().Be(NewPhotoUrl);
        await _userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsEntityNotFoundAndDoesNotUpload()
    {
        _userManager.FindByIdAsync(CurrentUserId).Returns((User?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        await _cloudinaryService.DidNotReceive().UploadImageAsync(Arg.Any<IFormFile>(), Arg.Any<CancellationToken>());
        await _userManager.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }
}