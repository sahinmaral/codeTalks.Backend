namespace codeTalks.Application.Services.FileStorage;

public static class FileStorageHelpers
{
    public static string ConvertPhotoPathToPublicId(string photoPath)
    {
        // Take everything after the "/upload/" segment of a Cloudinary URL or path.
        int uploadIndex = photoPath.IndexOf("/upload/", StringComparison.Ordinal);
        string publicId = uploadIndex >= 0
            ? photoPath[(uploadIndex + "/upload/".Length)..]
            : photoPath;

        // Strip a leading version segment such as "v1234567890/".
        int firstSlash = publicId.IndexOf('/');
        if (firstSlash > 1 && publicId[0] == 'v' && publicId[1..firstSlash].All(char.IsDigit))
        {
            publicId = publicId[(firstSlash + 1)..];
        }

        // Drop the file extension.
        int lastDot = publicId.LastIndexOf('.');
        return lastDot >= 0 ? publicId[..lastDot] : publicId;
    }
}