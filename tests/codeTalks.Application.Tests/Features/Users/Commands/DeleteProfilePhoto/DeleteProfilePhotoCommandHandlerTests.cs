using codeTalks.Application.Features.Auths.Rules;
using codeTalks.Application.Features.Users.Commands.DeleteProfilePhoto;
using codeTalks.Application.Services;
using codeTalks.Application.Services.FileStorage;
using codeTalks.Application.Tests.TestUtilities;
using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Users.Commands.DeleteProfilePhoto;

// Deletes the current user's profile photo: removes the Cloudinary image (by public id
// derived from its URL) and clears ProfilePhotoURL. If there's no photo, it's a business error.
// The command has no input fields, so there is nothing to validate (no validator).
public class DeleteProfilePhotoCommandHandlerTests
{
    private const string CurrentUserId = "current-user";

    private readonly ICloudinaryService _cloudinaryService = Substitute.For<ICloudinaryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly UserManager<User> _userManager = UserManagerMock.Create();
    private readonly DeleteProfilePhotoCommand.DeleteProfilePhotoCommandHandler _handler;

    public DeleteProfilePhotoCommandHandlerTests()
    {
        _currentUserService.GetCurrentUserIdAsync().Returns(CurrentUserId);
        var authBusinessRules = new AuthBusinessRules(_userManager);
        _handler = new DeleteProfilePhotoCommand.DeleteProfilePhotoCommandHandler(
            _cloudinaryService, _currentUserService, _userManager, authBusinessRules);
    }

    [Fact]
    public async Task Handle_WhenUserHasPhoto_DeletesImageByPublicIdAndClearsUrl()
    {
        var user = new User
        {
            Id = CurrentUserId,
            ProfilePhotoURL = "https://res.cloudinary.com/demo/image/upload/v1234567890/profile/oldphoto.png"
        };
        _userManager.FindByIdAsync(CurrentUserId).Returns(user);

        await _handler.Handle(new DeleteProfilePhotoCommand(), CancellationToken.None);

        await _cloudinaryService.Received(1).DeleteImageAsync("profile/oldphoto", Arg.Any<CancellationToken>());
        user.ProfilePhotoURL.Should().BeNull();
        await _userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoPhoto_ThrowsBusinessAndDoesNothing()
    {
        var user = new User { Id = CurrentUserId, ProfilePhotoURL = null };
        _userManager.FindByIdAsync(CurrentUserId).Returns(user);

        var act = () => _handler.Handle(new DeleteProfilePhotoCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*haven't uploaded any profile photo*");
        await _cloudinaryService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _userManager.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsEntityNotFound()
    {
        _userManager.FindByIdAsync(CurrentUserId).Returns((User?)null);

        var act = () => _handler.Handle(new DeleteProfilePhotoCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        await _cloudinaryService.DidNotReceive().DeleteImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}