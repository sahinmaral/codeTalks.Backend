using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using codeTalks.Application.Services.FileStorage;
using Core.CrossCuttingConcerns.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace codeTalks.Infrastructure.FileStorage;

public class CloudinaryService : ICloudinaryService
{
    readonly Cloudinary _cloudinary;
    
    public CloudinaryService(IOptions<CloudinaryOptions> cloudinaryOptions)
    {
        CloudinaryOptions cloudinaryOptionsValue = cloudinaryOptions.Value;

        Account account = new Account(
            cloudinaryOptionsValue.CloudName,
            cloudinaryOptionsValue.APIKey,
            cloudinaryOptionsValue.APISecret
        );

        _cloudinary = new Cloudinary(account);
    }

    public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, file.OpenReadStream()),
            Folder = "code-talks/images",
            PublicId = Guid.NewGuid().ToString()
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        EnsureSuccess(uploadResult, "The image could not be uploaded. Please try again later.");

        return uploadResult;
    }

    public async Task<DeletionResult?> DeleteImageAsync(string publicId, CancellationToken cancellationToken)
    {
        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };

        var deletionResult = await _cloudinary.DestroyAsync(deletionParams);
        EnsureSuccess(deletionResult, "The image could not be deleted. Please try again later.");

        return deletionResult;
    }

    private static void EnsureSuccess(BaseResult result, string errorMessage)
    {
        if ((int)result.StatusCode is >= 200 and < 300 && result.Error is null)
            return;

        // The user-facing message is a localization key (resolved in ExceptionMiddleware). The raw
        // Cloudinary error is external and not translatable, so it is not surfaced to the client.
        throw new BusinessException(errorMessage);
    }
}