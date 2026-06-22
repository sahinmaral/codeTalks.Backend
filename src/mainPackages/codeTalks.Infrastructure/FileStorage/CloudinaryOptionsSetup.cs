using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace codeTalks.Infrastructure.FileStorage;

public sealed class CloudinaryOptionsSetup(IConfiguration configuration) : IConfigureOptions<CloudinaryOptions>
{
    public void Configure(CloudinaryOptions options)
    {
        configuration.GetSection("Cloudinary").Bind(options);
    }
}