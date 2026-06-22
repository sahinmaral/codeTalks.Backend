using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace codeTalks.Application.Services.FileStorage;

public static class ImageFileRules
{
    private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

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