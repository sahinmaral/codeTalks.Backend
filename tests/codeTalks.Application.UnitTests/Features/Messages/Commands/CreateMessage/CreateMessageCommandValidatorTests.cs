using codeTalks.Application.Features.Messages.Commands.CreateMessage;
using FluentValidation.TestHelper;

namespace codeTalks.Application.UnitTests.Features.Messages.Commands.CreateMessage;

public class CreateMessageCommandValidatorTests
{
    private readonly CreateMessageCommandValidator _validator = new();

    private static CreateMessageCommand CommandWith(string content = "Hello", string channelId = "channel-1") =>
        new() { Content = content, ChannelId = channelId };

    [Fact]
    public void Validate_WhenAllFieldsValid_HasNoErrors()
    {
        var result = _validator.TestValidate(CommandWith());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenContentMissing_HasError(string? content)
    {
        var result = _validator.TestValidate(CommandWith(content: content!));

        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void Validate_WhenContentExceedsMaxLength_HasError()
    {
        var result = _validator.TestValidate(CommandWith(content: new string('x', 2001)));

        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void Validate_WhenChannelIdEmpty_HasError()
    {
        var result = _validator.TestValidate(CommandWith(channelId: ""));

        result.ShouldHaveValidationErrorFor(c => c.ChannelId);
    }
}