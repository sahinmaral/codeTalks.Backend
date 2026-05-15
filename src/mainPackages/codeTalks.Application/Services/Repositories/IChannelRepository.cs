using codeTalks.Domain;
using Core.Persistence.Repositories;

namespace codeTalks.Application.Services.Repositories;

public interface IChannelRepository : IAsyncRepository<Channel>, IRepository<Channel>
{
    
}