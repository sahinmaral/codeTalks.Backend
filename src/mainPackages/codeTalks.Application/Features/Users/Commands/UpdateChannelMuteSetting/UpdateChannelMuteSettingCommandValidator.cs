using FluentValidation;

namespace codeTalks.Application.Features.Users.Commands.UpdateChannelMuteSetting;

public class UpdateChannelMuteSettingCommandValidator : AbstractValidator<UpdateChannelMuteSettingCommand>
{
    public UpdateChannelMuteSettingCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();
    }
}