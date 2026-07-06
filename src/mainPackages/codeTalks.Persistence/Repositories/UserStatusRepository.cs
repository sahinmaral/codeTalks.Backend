using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using Core.Persistence.Repositories;

namespace codeTalks.Persistence.Repositories;

public sealed class UserStatusRepository(AppDbContext context) : EfRepositoryBase<UserStatus, AppDbContext>(context), IUserStatusRepository
{
    
}