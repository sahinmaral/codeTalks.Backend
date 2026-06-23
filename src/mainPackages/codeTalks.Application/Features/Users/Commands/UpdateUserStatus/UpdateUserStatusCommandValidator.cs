using FluentValidation;

namespace codeTalks.Application.Features.Users.Commands.UpdateUserStatus;

public class UpdateUserStatusCommandValidator : AbstractValidator<UpdateUserStatusCommand>
{
    public UpdateUserStatusCommandValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be between [0-3]");
    }
}