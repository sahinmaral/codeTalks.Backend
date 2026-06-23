using codeTalks.Application.Features.Channels.Commands.JoinChannel;
using codeTalks.Domain;
using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.JoinChannel;

public class JoinChannelCommandValidator : AbstractValidator<JoinChannelCommand>
{
    public JoinChannelCommandValidator()
    {
        RuleFor(x => x.InviteCode)
            .NotEmpty();
    }
}