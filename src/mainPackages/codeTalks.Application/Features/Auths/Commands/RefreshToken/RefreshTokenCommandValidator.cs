using FluentValidation;

namespace codeTalks.Application.Features.Auths.Commands.RefreshToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .NotEqual(Guid.Empty.ToString());
        
        RuleFor(x => x.UserId)
            .NotEmpty()
            .NotEqual(Guid.Empty.ToString());
    }
}