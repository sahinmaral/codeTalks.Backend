using codeTalks.Application.Features.Users.Dtos;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Persistence.Repositories;

public sealed class UserChannelMuteSettingRepository(AppDbContext context)
    : EfRepositoryBase<UserChannelMuteSetting, AppDbContext>(context), IUserChannelMuteSettingRepository
{
    public Task<List<UserChannelMuteSetting>> GetAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Query()
            .Where(setting => setting.UserId == userId)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public Task<UserChannelMuteSetting?> GetAsync(string userId, string channelId, CancellationToken ct = default)
    {
        return Query()
            .FirstOrDefaultAsync(setting => setting.UserId == userId && setting.ChannelId == channelId, ct);
    }
}