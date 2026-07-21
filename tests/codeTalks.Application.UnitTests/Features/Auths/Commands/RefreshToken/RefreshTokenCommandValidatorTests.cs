using codeTalks.Application.Features.Auths.Commands.RefreshToken;
using FluentValidation.TestHelper;

namespace codeTalks.Application.UnitTests.Features.Auths.Commands.RefreshToken;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenRefreshTokenIsAValidValue_HasNoError()
    {
        var result = _validator.TestValidate(new RefreshTokenCommand { RefreshToken = Guid.NewGuid().ToString() });

        result.ShouldNotHaveValidationErrorFor(c => c.RefreshToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenRefreshTokenMissing_HasError(string? refreshToken)
    {
        var result = _validator.TestValidate(new RefreshTokenCommand { RefreshToken = refreshToken! });

        result.ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }

    [Fact]
    public void Validate_WhenRefreshTokenIsEmptyGuid_HasError()
    {
        // The empty GUID is treated as a non-token; it must be rejected.
        var result = _validator.TestValidate(new RefreshTokenCommand { RefreshToken = Guid.Empty.ToString() });

        result.ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }
}