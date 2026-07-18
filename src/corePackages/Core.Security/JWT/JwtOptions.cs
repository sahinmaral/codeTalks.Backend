using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Core.Security.JWT;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string SecurityKey { get; set; } = null!;
    public int RefreshTokenExpirationInDays { get; set; }
}

public sealed class JwtOptionsSetup(IConfiguration configuration) : IConfigureOptions<JwtOptions>
{
    public void Configure(JwtOptions options)
    {
        configuration.GetSection("JwtOptions").Bind(options);
    }
}