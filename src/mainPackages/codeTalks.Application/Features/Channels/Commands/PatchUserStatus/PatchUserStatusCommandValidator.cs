using codeTalks.Domain;
using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.PatchUserStatus;

public class PatchUserStatusCommandValidator : AbstractValidator<PatchUserStatusCommand>
{
    public PatchUserStatusCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();

        RuleFor(x => x.Status)
            .NotNull()
            .Must(s => s == ChannelUserStatus.Accepted || s == ChannelUserStatus.Denied || s == ChannelUserStatus.Banned)
            .WithMessage("Status must be Accepted, Denied or Banned.");
    }
}