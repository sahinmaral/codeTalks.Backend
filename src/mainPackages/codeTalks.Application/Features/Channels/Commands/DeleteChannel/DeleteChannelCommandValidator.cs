using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.DeleteChannel;

public class DeleteChannelCommandValidator : AbstractValidator<DeleteChannelCommand>
{
    public DeleteChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();
    }
}