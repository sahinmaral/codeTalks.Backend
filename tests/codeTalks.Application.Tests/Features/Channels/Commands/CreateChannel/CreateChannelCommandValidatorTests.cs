using codeTalks.Application.Features.Channels.Commands.CreateChannel;
using codeTalks.Domain;
using FluentValidation.TestHelper;

namespace codeTalks.Application.Tests.Features.Channels.Commands.CreateChannel;

public class CreateChannelCommandValidatorTests
{
    private readonly CreateChannelCommandValidator _validator = new();

    private static CreateChannelCommand CommandWith(
        string name = "General",
        string description = "General chat",
        ChannelJoinPolicy joinPolicy = ChannelJoinPolicy.Request) =>
        new() { Name = name, Description = description, JoinPolicy = joinPolicy };

    [Fact]
    public void Validate_WhenAllFieldsValid_HasNoErrors()
    {
        var result = _validator.TestValidate(CommandWith());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]                    // NotEmpty
    [InlineData(null)]                  // NotEmpty
    public void Validate_WhenNameMissing_HasError(string? name)
    {
        var result = _validator.TestValidate(CommandWith(name: name!));

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_HasError()
    {
        var result = _validator.TestValidate(CommandWith(name: new string('x', 101)));

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenDescriptionMissing_HasError(string? description)
    {
        var result = _validator.TestValidate(CommandWith(description: description!));

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_HasError()
    {
        var result = _validator.TestValidate(CommandWith(description: new string('x', 501)));

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData(ChannelJoinPolicy.Open)]
    [InlineData(ChannelJoinPolicy.Request)]
    public void Validate_WhenJoinPolicyIsValid_HasNoJoinPolicyError(ChannelJoinPolicy joinPolicy)
    {
        var result = _validator.TestValidate(CommandWith(joinPolicy: joinPolicy));

        result.ShouldNotHaveValidationErrorFor(c => c.JoinPolicy);
    }

    [Fact]
    public void Validate_WhenJoinPolicyIsOutOfRange_HasError()
    {
        var result = _validator.TestValidate(CommandWith(joinPolicy: (ChannelJoinPolicy)99));

        result.ShouldHaveValidationErrorFor(c => c.JoinPolicy);
    }
}