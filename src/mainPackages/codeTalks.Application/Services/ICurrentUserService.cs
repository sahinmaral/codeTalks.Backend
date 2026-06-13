namespace codeTalks.Application.Services;

public interface ICurrentUserService
{
    Task<string> GetCurrentUserIdAsync();
}
