using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.UpdateChannel;

public class UpdateChannelCommandValidator : AbstractValidator<UpdateChannelCommand>
{
    public UpdateChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();

        RuleFor(x => x.UpdateChannelDto)
            .NotNull();

        RuleFor(x => x.UpdateChannelDto.Name)
            .MaximumLength(100)
            .When(x => x.UpdateChannelDto?.Name is not null);

        RuleFor(x => x.UpdateChannelDto.Description)
            .MaximumLength(500)
            .When(x => x.UpdateChannelDto?.Description is not null);
    }
}