using codeTalks.Application.Services.FileStorage;
using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.UpdateThumbnailPhoto;

public class UpdateThumbnailPhotoCommandValidator : AbstractValidator<UpdateThumbnailPhotoCommand>
{
    public UpdateThumbnailPhotoCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();

        RuleFor(x => x.Image).MustBeValidImage();
    }
}