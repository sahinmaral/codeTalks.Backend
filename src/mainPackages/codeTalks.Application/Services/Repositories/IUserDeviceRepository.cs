using System.Linq.Expressions;
using codeTalks.Domain;
using Core.Persistence.Repositories;

namespace codeTalks.Application.Services.Repositories;

public interface IUserDeviceRepository : IAsyncRepository<UserDevice>, IRepository<UserDevice>
{
    Task<List<UserDevice>> GetAllAsync(
        Expression<Func<UserDevice, bool>> predicate,
        CancellationToken cancellationToken = default);
}