using codeTalks.Application.Features.Channels.Commands.PatchUserStatus;
using codeTalks.Domain;
using FluentValidation.TestHelper;

namespace codeTalks.Application.UnitTests.Features.Channels.Commands.PatchUserStatus;

public class PatchUserStatusCommandValidatorTests
{
    private readonly PatchUserStatusCommandValidator _validator = new();

    private static PatchUserStatusCommand CommandWith(
        string channelId = "channel-1",
        ChannelUserStatus status = ChannelUserStatus.Accepted) =>
        new() { ChannelId = channelId, Status = status };

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

    [Theory]
    [InlineData(ChannelUserStatus.Accepted)]
    [InlineData(ChannelUserStatus.Denied)]
    [InlineData(ChannelUserStatus.Banned)]
    public void Validate_WhenStatusIsAllowed_HasNoStatusError(ChannelUserStatus status)
    {
        var result = _validator.TestValidate(CommandWith(status: status));

        result.ShouldNotHaveValidationErrorFor(c => c.Status);
    }

    [Theory]
    [InlineData(ChannelUserStatus.RequestSent)] // patching to RequestSent isn't allowed
    [InlineData((ChannelUserStatus)99)]         // out of enum range
    public void Validate_WhenStatusIsNotAllowed_HasError(ChannelUserStatus status)
    {
        var result = _validator.TestValidate(CommandWith(status: status));

        result.ShouldHaveValidationErrorFor(c => c.Status);
    }
}