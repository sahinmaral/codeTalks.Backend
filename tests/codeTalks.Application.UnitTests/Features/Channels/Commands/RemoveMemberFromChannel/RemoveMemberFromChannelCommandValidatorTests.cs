using codeTalks.Application.Features.Channels.Commands.RemoveMemberFromChannel;
using FluentValidation.TestHelper;

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.RemoveMemberFromChannel;

public class RemoveMemberFromChannelCommandValidatorTests
{
    private readonly RemoveMemberFromChannelCommandValidator _validator = new();

    private static RemoveMemberFromChannelCommand CommandWith(
        string channelId = "channel-1",
        string userId = "user-1") =>
        new() { ChannelId = channelId, UserId = userId };

    [Fact]
    public void Validate_WhenAllFieldsValid_HasNoErrors()
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
    public void Validate_WhenUserIdEmpty_HasError()
    {
        var result = _validator.TestValidate(CommandWith(userId: ""));

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}