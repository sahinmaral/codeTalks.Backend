using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace codeTalks.Application.Services.FileStorage;

public static class ImageFileRules
{
    public const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Transport cap for a photo upload request body: the file limit plus headroom for the
    /// multipart framing around it. Deliberately larger than <see cref="MaxFileSizeInBytes"/>
    /// so a slightly oversized file is rejected by this class's readable validation message
    /// rather than by Kestrel's bare 413.
    /// </summary>
    public const long MaxRequestBodySizeInBytes = MaxFileSizeInBytes + 512 * 1024;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif"
    ];

    public static IRuleBuilderOptions<T, IFormFile> MustBeValidImage<T>(
        this IRuleBuilder<T, IFormFile> ruleBuilder)
    {
        return ruleBuilder
            .NotNull().WithMessage("An image file is required")
            .Must(file => file is null || file.Length > 0).WithMessage("The image file cannot be empty")
            .Must(file => file is null || file.Length <= MaxFileSizeInBytes)
                .WithMessage($"The image file cannot exceed {MaxFileSizeInBytes / (1024 * 1024)} MB")
            .Must(file => file is null || AllowedContentTypes.Contains(file.ContentType))
                .WithMessage("The image must be a JPEG, PNG, WEBP or GIF file");
    }
}