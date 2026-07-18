using codeTalks.Application.Features.Users.Dtos;
using FluentValidation;

namespace codeTalks.Application.Features.Users.Commands.UpdateProfileInformation;

public class UpdateProfileInformationCommandValidator : AbstractValidator<UpdateProfileInformationCommand>
{
    public UpdateProfileInformationCommandValidator()
    {
        RuleFor(u => u.ProfileInformation.FirstName).NotEmpty();
        RuleFor(u => u.ProfileInformation.FirstName).MinimumLength(2);
        RuleFor(u => u.ProfileInformation.FirstName).MaximumLength(50);

        RuleFor(u => u.ProfileInformation.LastName).NotEmpty();
        RuleFor(u => u.ProfileInformation.LastName).MinimumLength(2);
        RuleFor(u => u.ProfileInformation.LastName).MaximumLength(50);

        RuleFor(u => u.ProfileInformation.MiddleName)
            .MinimumLength(2).When(u => !string.IsNullOrEmpty(u.ProfileInformation.MiddleName));
        RuleFor(u => u.ProfileInformation.MiddleName)
            .MaximumLength(50).When(u => !string.IsNullOrEmpty(u.ProfileInformation.MiddleName));

        RuleFor(u => u.ProfileInformation.Bio)
            .MinimumLength(2).When(u => !string.IsNullOrEmpty(u.ProfileInformation.Bio));
        RuleFor(u => u.ProfileInformation.Bio)
            .MaximumLength(300).When(u => !string.IsNullOrEmpty(u.ProfileInformation.Bio));
    }
}