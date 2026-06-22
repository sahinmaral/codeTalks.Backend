using codeTalks.Application.Services.FileStorage;
using FluentValidation;

namespace codeTalks.Application.Features.Users.Commands.UpdateProfilePhoto;

public class UpdateProfilePhotoCommandValidator : AbstractValidator<UpdateProfilePhotoCommand>
{
    public UpdateProfilePhotoCommandValidator()
    {
        RuleFor(x => x.Image).MustBeValidImage();
    }
}