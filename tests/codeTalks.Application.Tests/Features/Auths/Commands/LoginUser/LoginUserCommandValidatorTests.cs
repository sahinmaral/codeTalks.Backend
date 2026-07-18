using codeTalks.Application.Features.Auths.Commands.LoginUser;
using FluentValidation.TestHelper;

namespace codeTalks.Application.Tests.Features.Auths.Commands.LoginUser;

public class LoginUserCommandValidatorTests
{
    private readonly LoginUserCommandValidator _validator = new();

    private static LoginUserCommand CommandWith(string usernameOrEmail = "janedoe", string password = "secret1") =>
        new() { UsernameOrEmail = usernameOrEmail, Password = password };

    [Fact]
    public void Validate_WhenAllFieldsValid_HasNoErrors()
    {
        var result = _validator.TestValidate(CommandWith());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenUsernameOrEmailMissing_HasError(string? usernameOrEmail)
    {
        var result = _validator.TestValidate(CommandWith(usernameOrEmail: usernameOrEmail!));

        result.ShouldHaveValidationErrorFor(c => c.UsernameOrEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenPasswordMissing_HasError(string? password)
    {
        var result = _validator.TestValidate(CommandWith(password: password!));

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }
}