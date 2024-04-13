using Core.Security.Entities;

namespace Core.Security.JWT;

public interface IJwtProvider
{
    Task<TokenResponse> CreateTokenAsync(User user);
}
