using codeTalks.Application.Features.Users.Commands.UpdateChannelMuteSetting;
using FluentValidation.TestHelper;

namespace codeTalks.Application.Tests.Features.Users.Commands.UpdateChannelMuteSetting;

public class UpdateChannelMuteSettingCommandValidatorTests
{
    private readonly UpdateChannelMuteSettingCommandValidator _validator = new();

    private static UpdateChannelMuteSettingCommand CommandWith(string? channelId = "channel-1", DateTime? muteUntil = null) =>
        new() { ChannelId = channelId!, MuteUntil = muteUntil ?? DateTime.UtcNow.AddHours(1) };

    [Fact]
    public void Validate_WhenChannelIdAndFutureMuteUntil_HasNoErrors()
    {
        var result = _validator.TestValidate(CommandWith());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenChannelIdMissing_HasError(string? channelId)
    {
        var result = _validator.TestValidate(CommandWith(channelId: channelId));

        result.ShouldHaveValidationErrorFor(c => c.ChannelId);
    }

    [Fact]
    public void Validate_WhenMuteUntilIsInTheFuture_HasNoMuteUntilError()
    {
        var result = _validator.TestValidate(CommandWith(muteUntil: DateTime.UtcNow.AddDays(1)));

        result.ShouldNotHaveValidationErrorFor(c => c.MuteUntil);
    }

    [Fact]
    public void Validate_WhenMuteUntilIsInThePast_HasError()
    {
        var result = _validator.TestValidate(CommandWith(muteUntil: DateTime.UtcNow.AddHours(-1)));

        result.ShouldHaveValidationErrorFor(c => c.MuteUntil)
            .WithErrorMessage("Mute until must be a future date.");
    }
}