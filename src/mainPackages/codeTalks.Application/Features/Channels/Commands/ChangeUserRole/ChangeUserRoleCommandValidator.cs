using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.ChangeUserRole;

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => role is "User" or "Moderator" or "Owner")
            .WithMessage("Role must be User, Moderator or Owner.");
    }
}
