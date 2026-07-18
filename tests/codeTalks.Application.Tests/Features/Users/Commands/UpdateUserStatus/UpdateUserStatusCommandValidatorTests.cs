using codeTalks.Application.Features.Users.Commands.UpdateUserStatus;
using codeTalks.Domain;
using FluentValidation.TestHelper;

namespace codeTalks.Application.Tests.Features.Users.Commands.UpdateUserStatus;

public class UpdateUserStatusCommandValidatorTests
{
    private readonly UpdateUserStatusCommandValidator _validator = new();

    [Theory]
    [InlineData(UserStatusType.Online)]
    [InlineData(UserStatusType.Away)]
    [InlineData(UserStatusType.Busy)]
    [InlineData(UserStatusType.Invisible)]
    public void Validate_WhenStatusIsDefinedEnumValue_HasNoError(UserStatusType status)
    {
        var result = _validator.TestValidate(new UpdateUserStatusCommand { Status = status });

        result.ShouldNotHaveValidationErrorFor(c => c.Status);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(99)]
    [InlineData(-1)]
    public void Validate_WhenStatusIsOutsideEnumRange_HasError(int status)
    {
        // IsInEnum() must reject values that aren't defined on UserStatusType (0-3).
        var result = _validator.TestValidate(new UpdateUserStatusCommand { Status = (UserStatusType)status });

        result.ShouldHaveValidationErrorFor(c => c.Status);
    }
}