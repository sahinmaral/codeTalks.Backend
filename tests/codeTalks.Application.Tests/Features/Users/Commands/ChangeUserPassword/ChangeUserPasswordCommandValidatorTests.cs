using codeTalks.Application.Features.Users.Commands.ChangeUserPassword;
using FluentValidation.TestHelper;

namespace codeTalks.Application.Tests.Features.Users.Commands.ChangeUserPassword;

public class ChangeUserPasswordCommandValidatorTests
{
    private readonly ChangeUserPasswordCommandValidator _validator = new();

    private static ChangeUserPasswordCommand CommandWith(
        string currentPassword = "OldPassword1",
        string newPassword = "NewPassword1") =>
        new() { CurrentPassword = currentPassword, NewPassword = newPassword };

    [Fact]
    public void Validate_WhenAllFieldsValid_HasNoErrors()
    {
        var result = _validator.TestValidate(CommandWith());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCurrentPasswordEmpty_HasError()
    {
        var result = _validator.TestValidate(CommandWith(currentPassword: ""));

        result.ShouldHaveValidationErrorFor(c => c.CurrentPassword);
    }

    [Theory]
    [InlineData("")]        // NotEmpty
    [InlineData("12345")]   // MinimumLength(6)
    public void Validate_WhenNewPasswordInvalid_HasError(string newPassword)
    {
        var result = _validator.TestValidate(CommandWith(newPassword: newPassword));

        result.ShouldHaveValidationErrorFor(c => c.NewPassword);
    }

    [Fact]
    public void Validate_WhenNewPasswordEqualsCurrent_HasError()
    {
        var result = _validator.TestValidate(CommandWith(currentPassword: "SamePass1", newPassword: "SamePass1"));

        result.ShouldHaveValidationErrorFor(c => c.NewPassword)
            .WithErrorMessage("New password must be different from the current password");
    }
}