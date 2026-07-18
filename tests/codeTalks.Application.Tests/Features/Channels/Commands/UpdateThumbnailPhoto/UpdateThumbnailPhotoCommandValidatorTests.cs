using codeTalks.Application.Features.Channels.Commands.UpdateThumbnailPhoto;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Channels.Commands.UpdateThumbnailPhoto;

// Validates ChannelId plus the shared MustBeValidImage() rule (exhaustively covered in
// UpdateProfilePhotoCommandValidatorTests; here we confirm the composition on this command).
public class UpdateThumbnailPhotoCommandValidatorTests
{
    private readonly UpdateThumbnailPhotoCommandValidator _validator = new();

    private static IFormFile ValidImage()
    {
        var file = Substitute.For<IFormFile>();
        file.Length.Returns(1024);
        file.ContentType.Returns("image/png");
        return file;
    }

    private static UpdateThumbnailPhotoCommand CommandWith(string channelId = "channel-1", IFormFile? image = null) =>
        new() { ChannelId = channelId, Image = image ?? ValidImage() };

    [Fact]
    public void Validate_WhenChannelIdAndImageValid_HasNoErrors()
    {
        var result = _validator.TestValidate(CommandWith());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenChannelIdEmpty_HasError()
    {
        var result = _validator.TestValidate(CommandWith(channelId: ""));

        result.ShouldHaveValidationErrorFor(c => c.ChannelId);
    }

    [Fact]
    public void Validate_WhenImageIsNull_HasError()
    {
        // Construct directly rather than via CommandWith, whose `?? ValidImage()` would mask a null.
        var command = new UpdateThumbnailPhotoCommand { ChannelId = "channel-1", Image = null! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Image);
    }

    [Fact]
    public void Validate_WhenImageContentTypeNotAllowed_HasError()
    {
        var badImage = Substitute.For<IFormFile>();
        badImage.Length.Returns(1024);
        badImage.ContentType.Returns("application/pdf");

        var result = _validator.TestValidate(CommandWith(image: badImage));

        result.ShouldHaveValidationErrorFor(c => c.Image);
    }
}