using codeTalks.Domain;
using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.CreateChannel;

public class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.JoinPolicy)
            .Must(p => p == ChannelJoinPolicy.Open || p == ChannelJoinPolicy.Request)
            .WithMessage("Join Policy must be Open or Request.");
    }
}