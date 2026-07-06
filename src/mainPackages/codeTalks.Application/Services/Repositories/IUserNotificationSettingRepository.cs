using codeTalks.Domain;
using Core.Persistence.Repositories;

namespace codeTalks.Application.Services.Repositories;

public interface IUserNotificationSettingRepository : IAsyncRepository<UserNotificationSetting>,
    IRepository<UserNotificationSetting>
{
    Task<UserNotificationSetting?> GetByUserIdAsync(string userId, CancellationToken ct = default);
}