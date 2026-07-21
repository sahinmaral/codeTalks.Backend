using codeTalks.Application.Features.Channels.Commands.DeleteThumbnailPhoto;
using FluentValidation.TestHelper;

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.DeleteThumbnailPhoto;

public class DeleteThumbnailPhotoCommandValidatorTests
{
    private readonly DeleteThumbnailPhotoCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenChannelIdProvided_HasNoError()
    {
        var result = _validator.TestValidate(new DeleteThumbnailPhotoCommand { ChannelId = "channel-1" });

        result.ShouldNotHaveValidationErrorFor(c => c.ChannelId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenChannelIdMissing_HasError(string? channelId)
    {
        var result = _validator.TestValidate(new DeleteThumbnailPhotoCommand { ChannelId = channelId! });

        result.ShouldHaveValidationErrorFor(c => c.ChannelId);
    }
}