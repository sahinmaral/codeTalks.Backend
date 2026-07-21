using codeTalks.Application.Features.Users.Commands.UnmuteChannel;
using FluentValidation.TestHelper;

namespace codeTalks.Application.UnitTests.Features.Users.Commands.UnmuteChannel;

public class UnmuteChannelCommandValidatorTests
{
    private readonly UnmuteChannelCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenChannelIdProvided_HasNoError()
    {
        var result = _validator.TestValidate(new UnmuteChannelCommand { ChannelId = "channel-1" });

        result.ShouldNotHaveValidationErrorFor(c => c.ChannelId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenChannelIdMissing_HasError(string? channelId)
    {
        var result = _validator.TestValidate(new UnmuteChannelCommand { ChannelId = channelId! });

        result.ShouldHaveValidationErrorFor(c => c.ChannelId);
    }
}