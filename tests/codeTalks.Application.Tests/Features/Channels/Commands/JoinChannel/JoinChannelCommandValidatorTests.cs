using codeTalks.Application.Features.Channels.Commands.JoinChannel;
using FluentValidation.TestHelper;

namespace codeTalks.Application.Tests.Features.Channels.Commands.JoinChannel;

public class JoinChannelCommandValidatorTests
{
    private readonly JoinChannelCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenInviteCodeProvided_HasNoError()
    {
        var result = _validator.TestValidate(new JoinChannelCommand { InviteCode = "abc123" });

        result.ShouldNotHaveValidationErrorFor(c => c.InviteCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenInviteCodeMissing_HasError(string? inviteCode)
    {
        var result = _validator.TestValidate(new JoinChannelCommand { InviteCode = inviteCode! });

        result.ShouldHaveValidationErrorFor(c => c.InviteCode);
    }
}