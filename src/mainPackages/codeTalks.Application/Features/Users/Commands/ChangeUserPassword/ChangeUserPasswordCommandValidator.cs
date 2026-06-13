using FluentValidation;

namespace codeTalks.Application.Features.Users.Commands.ChangeUserPassword;

public class ChangeUserPasswordCommandValidator : AbstractValidator<ChangeUserPasswordCommand>
{
    public ChangeUserPasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty();
        RuleFor(x => x.NewPassword)
            .MinimumLength(6);
        RuleFor(x => x.NewPassword)
            .MaximumLength(100);
        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from the current password");
    }
}