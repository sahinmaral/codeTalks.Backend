using codeTalks.Application.Features.Channels.Commands.ChangeUserRole;
using FluentValidation.TestHelper;

namespace codeTalks.Application.Tests.Features.Channels.Commands.ChangeUserRole;

public class ChangeUserRoleCommandValidatorTests
{
    private readonly ChangeUserRoleCommandValidator _validator = new();

    private static ChangeUserRoleCommand CommandWith(
        string channelId = "channel-1",
        string userId = "user-1",
        string role = "Moderator") =>
        new() { ChannelId = channelId, UserId = userId, Role = role };

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

    [Theory]
    [InlineData("User")]
    [InlineData("Moderator")]
    [InlineData("Owner")]
    public void Validate_WhenRoleIsAllowed_HasNoRoleError(string role)
    {
        var result = _validator.TestValidate(CommandWith(role: role));

        result.ShouldNotHaveValidationErrorFor(c => c.Role);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin")]
    [InlineData("moderator")] // case-sensitive: only exact "Moderator" is allowed
    public void Validate_WhenRoleIsNotAllowed_HasError(string role)
    {
        var result = _validator.TestValidate(CommandWith(role: role));

        result.ShouldHaveValidationErrorFor(c => c.Role);
    }
}