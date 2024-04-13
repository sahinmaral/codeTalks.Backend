using codeTalks.Domain;
using Core.Persistence.Repositories;

namespace codeTalks.Application.Services.Repositories;

public interface IMessageRepository : IAsyncRepository<Message>, IRepository<Message>
{
    
}