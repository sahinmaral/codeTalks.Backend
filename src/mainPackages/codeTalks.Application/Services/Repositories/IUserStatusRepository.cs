using codeTalks.Domain;
using Core.Persistence.Repositories;

namespace codeTalks.Application.Services.Repositories;

public interface IUserStatusRepository : IAsyncRepository<UserStatus>, IRepository<UserStatus>
{
    
}