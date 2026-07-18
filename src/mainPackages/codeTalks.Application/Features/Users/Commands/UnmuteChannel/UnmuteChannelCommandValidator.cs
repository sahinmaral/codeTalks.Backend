using FluentValidation;

namespace codeTalks.Application.Features.Users.Commands.UnmuteChannel;

public class UnmuteChannelCommandValidator : AbstractValidator<UnmuteChannelCommand>
{
    public UnmuteChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();
    }
}