using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Domain;
using Core.Persistence.Repositories;

namespace codeTalks.Application.Services.Repositories;

public interface IUserChannelMuteSettingRepository : IAsyncRepository<UserChannelMuteSetting>,
    IRepository<UserChannelMuteSetting>
{
    Task<List<UserChannelMuteSetting>> GetAllAsync(string userId, CancellationToken ct = default);
    Task<UserChannelMuteSetting?> GetAsync(string userId, string channelId, CancellationToken ct = default);
}