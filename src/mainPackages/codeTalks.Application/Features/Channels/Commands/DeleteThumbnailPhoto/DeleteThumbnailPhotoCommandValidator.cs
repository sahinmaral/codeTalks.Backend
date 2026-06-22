using FluentValidation;

namespace codeTalks.Application.Features.Channels.Commands.DeleteThumbnailPhoto;

public class DeleteThumbnailPhotoCommandValidator : AbstractValidator<DeleteThumbnailPhotoCommand>
{
    public DeleteThumbnailPhotoCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
    }
}