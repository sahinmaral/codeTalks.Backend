using FluentValidation;

namespace codeTalks.Application.Features.Users.Commands.UpdateChannelMuteSetting;

public class UpdateChannelMuteSettingCommandValidator : AbstractValidator<UpdateChannelMuteSettingCommand>
{
    public UpdateChannelMuteSettingCommandValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty();

        // Evaluated per-validation (not a captured constant) so it always compares against the
        // current UTC time; muting until a past/now time is meaningless (IsMuted would be false).
        RuleFor(x => x.MuteUntil)
            .Must(muteUntil => muteUntil > DateTime.UtcNow)
            .WithMessage("Mute until must be a future date.");
    }
}