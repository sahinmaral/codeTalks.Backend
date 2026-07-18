using codeTalks.Application.Features.Channels.Commands.DeleteChannel;
using FluentValidation.TestHelper;

namespace codeTalks.Application.Tests.Features.Channels.Commands.DeleteChannel;

public class DeleteChannelCommandValidatorTests
{
    private readonly DeleteChannelCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenChannelIdProvided_HasNoError()
    {
        var result = _validator.TestValidate(new DeleteChannelCommand { ChannelId = "channel-1" });

        result.ShouldNotHaveValidationErrorFor(c => c.ChannelId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenChannelIdMissing_HasError(string? channelId)
    {
        var result = _validator.TestValidate(new DeleteChannelCommand { ChannelId = channelId! });

        result.ShouldHaveValidationErrorFor(c => c.ChannelId);
    }
}