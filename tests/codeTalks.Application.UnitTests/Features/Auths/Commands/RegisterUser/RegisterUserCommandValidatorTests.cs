using codeTalks.Application.Features.Auths.Commands.RegisterUser;
using FluentValidation.TestHelper;

namespace codeTalks.Application.UnitTests.Features.Auths.Commands.RegisterUser;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    // Required fields only; optional ProfilePhotoURL/Bio/MiddleName left null on purpose.
    private static RegisterUserCommand ValidCommand() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        UserName = "janedoe",
        Email = "jane@example.com",
        Password = "secret1"
    };

    [Fact]
    public void Validate_WhenRequiredFieldsValid_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]   // NotEmpty
    [InlineData("A")]  // MinimumLength(2)
    public void Validate_WhenFirstNameInvalid_HasError(string firstName)
    {
        var command = ValidCommand();
        command.FirstName = firstName;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.FirstName);
    }

    [Fact]
    public void Validate_WhenLastNameEmpty_HasError()
    {
        var command = ValidCommand();
        command.LastName = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.LastName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Validate_WhenUserNameInvalid_HasError(string userName)
    {
        var command = ValidCommand();
        command.UserName = userName;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.UserName);
    }

    [Theory]
    [InlineData("")]                 // NotEmpty
    [InlineData("not-an-email")]     // EmailAddress
    public void Validate_WhenEmailInvalid_HasError(string email)
    {
        var command = ValidCommand();
        command.Email = email;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("")]        // NotEmpty
    [InlineData("12345")]   // MinimumLength(6)
    public void Validate_WhenPasswordInvalid_HasError(string password)
    {
        var command = ValidCommand();
        command.Password = password;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Validate_WhenOptionalFieldsProvidedAndValid_HasNoErrors()
    {
        var command = ValidCommand();
        command.MiddleName = "Ann";

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}