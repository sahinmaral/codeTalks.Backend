using codeTalks.Domain;
using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.PatchChannel;

public class PatchChannelCommandValidator : AbstractValidator<PatchChannelCommand>
{
    public PatchChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();

        RuleFor(x => x.PatchChannelDto)
            .NotNull();

        RuleFor(x => x.PatchChannelDto.Name)
            .MaximumLength(100)
            .When(x => x.PatchChannelDto.Name is not null);

        RuleFor(x => x.PatchChannelDto.Description)
            .MaximumLength(500)
            .When(x => x.PatchChannelDto.Description is not null);
        
        RuleFor(x => x.PatchChannelDto.JoinPolicy)
            .Must(s => s == ChannelJoinPolicy.Open || s == ChannelJoinPolicy.Request)
            .WithMessage("Join Policy must be Open or Request.")
            .When(x => x.PatchChannelDto.Name is not null);
    }
}