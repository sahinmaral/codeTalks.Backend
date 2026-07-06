using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Persistence.Repositories;

public sealed class UserNotificationSettingRepository(AppDbContext context)
    : EfRepositoryBase<UserNotificationSetting, AppDbContext>(context), IUserNotificationSettingRepository
{
    public Task<UserNotificationSetting?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return Query()
            .FirstOrDefaultAsync(setting => setting.UserId == userId, ct);
    }
}