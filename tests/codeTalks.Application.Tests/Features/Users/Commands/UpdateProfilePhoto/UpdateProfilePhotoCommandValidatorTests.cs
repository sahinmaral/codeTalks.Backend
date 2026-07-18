using codeTalks.Application.Features.Users.Commands.UpdateProfilePhoto;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace codeTalks.Application.Tests.Features.Users.Commands.UpdateProfilePhoto;

// Exercises the shared MustBeValidImage() rule via this command: required, non-empty,
// at most 5 MB, and one of the allowed image content types.
public class UpdateProfilePhotoCommandValidatorTests
{
    private const long FiveMb = 5 * 1024 * 1024;

    private readonly UpdateProfilePhotoCommandValidator _validator = new();

    private static IFormFile Image(long length = 1024, string contentType = "image/png")
    {
        var file = Substitute.For<IFormFile>();
        file.Length.Returns(length);
        file.ContentType.Returns(contentType);
        return file;
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void Validate_WhenImageIsValid_HasNoError(string contentType)
    {
        var command = new UpdateProfilePhotoCommand { Image = Image(contentType: contentType) };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Image);
    }

    [Fact]
    public void Validate_WhenImageIsNull_HasError()
    {
        var command = new UpdateProfilePhotoCommand { Image = null! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Image).WithErrorMessage("An image file is required");
    }

    [Fact]
    public void Validate_WhenImageIsEmpty_HasError()
    {
        var command = new UpdateProfilePhotoCommand { Image = Image(length: 0) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Image).WithErrorMessage("The image file cannot be empty");
    }

    [Fact]
    public void Validate_WhenImageExceedsMaxSize_HasError()
    {
        var command = new UpdateProfilePhotoCommand { Image = Image(length: FiveMb + 1) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Image);
    }

    [Fact]
    public void Validate_WhenContentTypeIsNotAllowed_HasError()
    {
        var command = new UpdateProfilePhotoCommand { Image = Image(contentType: "application/pdf") };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Image);
    }
}