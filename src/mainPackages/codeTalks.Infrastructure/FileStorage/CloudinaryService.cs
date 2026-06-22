using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using codeTalks.Application.Services.FileStorage;
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

        return await _cloudinary.UploadAsync(uploadParams);
    }

    public async Task<DeletionResult?> DeleteImageAsync(string publicId, CancellationToken cancellationToken)
    {
        var deletionParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };

        return await _cloudinary.DestroyAsync(deletionParams);
    }
}