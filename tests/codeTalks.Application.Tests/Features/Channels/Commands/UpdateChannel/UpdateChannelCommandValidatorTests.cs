using codeTalks.Application.Features.Channels.Commands.UpdateChannel;
using codeTalks.Application.Features.Channels.Dtos;
using FluentValidation.TestHelper;

namespace codeTalks.Application.Tests.Features.Channels.Commands.UpdateChannel;

public class UpdateChannelCommandValidatorTests
{
    private readonly UpdateChannelCommandValidator _validator = new();

    private static UpdateChannelCommand CommandWith(string? name = "General", string? description = "General chat") =>
        new()
        {
            ChannelId = "channel-1",
            UpdateChannelDto = new UpdateChannelDto { Name = name!, Description = description! }
        };

    [Fact]
    public void Validate_WhenAllFieldsValid_HasNoErrors()
    {
        var result = _validator.TestValidate(CommandWith());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenChannelIdEmpty_HasError()
    {
        var command = CommandWith();
        command.ChannelId = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ChannelId);
    }

    [Fact]
    public void Validate_WhenDtoIsNull_HasErrorAndDoesNotThrow()
    {
        // The null-safe When guards must prevent the child rules from dereferencing a null DTO.
        var command = new UpdateChannelCommand { ChannelId = "channel-1", UpdateChannelDto = null! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.UpdateChannelDto);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasError()
    {
        var result = _validator.TestValidate(CommandWith(name: new string('x', 101)));

        result.ShouldHaveValidationErrorFor(c => c.UpdateChannelDto.Name);
    }

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_HasError()
    {
        var result = _validator.TestValidate(CommandWith(description: new string('x', 501)));

        result.ShouldHaveValidationErrorFor(c => c.UpdateChannelDto.Description);
    }

    [Fact]
    public void Validate_WhenNameIsNull_HasNoNameError()
    {
        // Null means "keep existing"; the length rule is skipped for a null value.
        var result = _validator.TestValidate(CommandWith(name: null));

        result.ShouldNotHaveValidationErrorFor(c => c.UpdateChannelDto.Name);
    }
}