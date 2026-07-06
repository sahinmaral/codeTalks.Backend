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
        EnsureSuccess(uploadResult, "image upload");

        return uploadResult;
    }

    public async Task<DeletionResult?> DeleteImageAsync(string publicId, CancellationToken cancellationToken)
    {
        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };

        var deletionResult = await _cloudinary.DestroyAsync(deletionParams);
        EnsureSuccess(deletionResult, "image deletion");

        return deletionResult;
    }

    private static void EnsureSuccess(BaseResult result, string operation)
    {
        if ((int)result.StatusCode is >= 200 and < 300 && result.Error is null)
            return;

        throw new BusinessException(
            $"Cloudinary {operation} failed: {result.Error?.Message ?? result.StatusCode.ToString()}");
    }
}