using FluentValidation;

namespace codeTalks.Application.Features.Auths.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(u => u.FirstName).NotEmpty();
        RuleFor(u => u.FirstName).MinimumLength(2);
        RuleFor(u => u.FirstName).MaximumLength(50);

        RuleFor(u => u.LastName).NotEmpty();
        RuleFor(u => u.LastName).MinimumLength(2);
        RuleFor(u => u.LastName).MaximumLength(50);

        RuleFor(u => u.UserName).NotEmpty();
        RuleFor(u => u.UserName).MinimumLength(2);
        RuleFor(u => u.UserName).MaximumLength(50);

        RuleFor(u => u.MiddleName)
            .MinimumLength(2).When(u => !string.IsNullOrEmpty(u.MiddleName));
        RuleFor(u => u.MiddleName)
            .MaximumLength(50).When(u => !string.IsNullOrEmpty(u.MiddleName));

        RuleFor(u => u.Email).NotEmpty();
        RuleFor(u => u.Email).EmailAddress();
        RuleFor(u => u.Email)
            .MaximumLength(320);

        RuleFor(u => u.Password).NotEmpty();
        RuleFor(u => u.Password).MinimumLength(6);
        RuleFor(u => u.Password).MaximumLength(100);
    }
}