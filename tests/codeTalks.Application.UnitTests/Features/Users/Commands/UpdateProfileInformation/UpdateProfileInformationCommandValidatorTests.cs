using codeTalks.Application.Features.Users.Commands.UpdateProfileInformation;
using codeTalks.Application.Features.Users.Dtos;
using FluentValidation.TestHelper;

namespace codeTalks.Application.UnitTests.Features.Users.Commands.UpdateProfileInformation;

// Pure unit tests: no mocks, no I/O. A validator is deterministic input -> errors,
// which makes it the cheapest and most valuable thing to unit-test in the app layer.
public class UpdateProfileInformationCommandValidatorTests
{
    private readonly UpdateProfileInformationCommandValidator _validator = new();

    private static UpdateProfileInformationCommand CommandWith(
        string firstName = "Jane",
        string lastName = "Doe",
        string? middleName = null,
        string? bio = null) =>
        new()
        {
            ProfileInformation = new UpdateProfileInformationDto
            {
                FirstName = firstName,
                LastName = lastName,
                MiddleName = middleName,
                Bio = bio
            }
        };

    [Fact]
    public void Validate_WhenAllFieldsValid_HasNoErrors()
    {
        var result = _validator.TestValidate(CommandWith());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]        // NotEmpty
    [InlineData("A")]       // MinimumLength(2)
    public void Validate_WhenFirstNameInvalid_HasError(string firstName)
    {
        var result = _validator.TestValidate(CommandWith(firstName: firstName));

        result.ShouldHaveValidationErrorFor(c => c.ProfileInformation.FirstName);
    }

    [Fact]
    public void Validate_WhenLastNameTooLong_HasError()
    {
        var result = _validator.TestValidate(CommandWith(lastName: new string('x', 51)));

        result.ShouldHaveValidationErrorFor(c => c.ProfileInformation.LastName);
    }

    [Fact]
    public void Validate_WhenMiddleNameNull_HasNoError()
    {
        // MiddleName is optional; rules only apply When it is not null/empty.
        var result = _validator.TestValidate(CommandWith(middleName: null));

        result.ShouldNotHaveValidationErrorFor(c => c.ProfileInformation.MiddleName);
    }

    [Fact]
    public void Validate_WhenBioProvidedButTooShort_HasError()
    {
        var result = _validator.TestValidate(CommandWith(bio: "x"));

        result.ShouldHaveValidationErrorFor(c => c.ProfileInformation.Bio);
    }
}