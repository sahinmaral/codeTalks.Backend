using codeTalks.Domain;
using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.RemoveMemberFromChannel;

public class RemoveMemberFromChannelCommandValidator : AbstractValidator<RemoveMemberFromChannelCommand>
{
    public RemoveMemberFromChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();
        
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}