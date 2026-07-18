using Core.Application.CQRS;
using Core.Application.Pipelines.Validation;
using FluentAssertions;
using FluentValidation;

namespace Core.Application.UnitTests.Pipelines;

// RequestValidationBehavior runs every registered validator, aggregates the failures, and
// throws ValidationException if there are any; otherwise it calls the next delegate.
public class RequestValidationBehaviorTests
{
    private static RequestValidationBehavior<ValidatableRequest, TestResponse> CreateBehavior(
        params IValidator<ValidatableRequest>[] validators) => new(validators);

    private static InlineValidator<ValidatableRequest> NameRequiredValidator()
    {
        var validator = new InlineValidator<ValidatableRequest>();
        validator.RuleFor(x => x.Name).NotEmpty();
        return validator;
    }

    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        var behavior = CreateBehavior();
        var response = new TestResponse { Value = "ok" };
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = () => { nextCalled = true; return Task.FromResult(response); };

        var result = await behavior.Handle(new ValidatableRequest { Name = "anything" }, next, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNext()
    {
        var behavior = CreateBehavior(NameRequiredValidator());
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = () => { nextCalled = true; return Task.FromResult(new TestResponse()); };

        await behavior.Handle(new ValidatableRequest { Name = "valid" }, next, CancellationToken.None);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsAndDoesNotCallNext()
    {
        var behavior = CreateBehavior(NameRequiredValidator());
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = () => { nextCalled = true; return Task.FromResult(new TestResponse()); };

        var act = () => behavior.Handle(new ValidatableRequest { Name = "" }, next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenMultipleValidatorsFail_AggregatesFailuresAcrossValidators()
    {
        // Two separate validators, each contributing one distinct failure.
        var alwaysFails = new InlineValidator<ValidatableRequest>();
        alwaysFails.RuleFor(x => x.Name).Must(_ => false).WithMessage("always fails");
        var behavior = CreateBehavior(NameRequiredValidator(), alwaysFails);
        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse());

        var act = () => behavior.Handle(new ValidatableRequest { Name = "" }, next, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        var messages = ex.Which.Errors.Select(e => e.ErrorMessage).ToList();
        messages.Should().Contain("always fails");                          // from the second validator
        messages.Should().Contain(m => m.Contains("must not be empty"));    // from the first validator
    }
}