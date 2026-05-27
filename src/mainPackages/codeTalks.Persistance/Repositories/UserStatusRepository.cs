using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistance.Contexts;
using Core.Persistence.Repositories;

namespace codeTalks.Persistance.Repositories;

public sealed class UserStatusRepository(AppDbContext context) : EfRepositoryBase<UserStatus, AppDbContext>(context), IUserStatusRepository
{
    
}