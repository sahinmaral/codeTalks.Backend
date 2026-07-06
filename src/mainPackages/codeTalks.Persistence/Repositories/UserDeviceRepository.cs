using System.Linq.Expressions;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Persistence.Repositories;

public sealed class UserDeviceRepository(AppDbContext context)
    : EfRepositoryBase<UserDevice, AppDbContext>(context), IUserDeviceRepository
{
    private readonly AppDbContext _context = context;
    
    public Task<List<UserDevice>> GetAllAsync(Expression<Func<UserDevice, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _context.Set<UserDevice>()
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }
}