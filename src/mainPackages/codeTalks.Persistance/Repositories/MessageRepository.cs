using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistance.Contexts;
using Core.Persistence.Repositories;

namespace codeTalks.Persistance.Repositories;

public sealed class MessageRepository :  EfRepositoryBase<Message, AppDbContext>, IMessageRepository
{
    public MessageRepository(AppDbContext context) : base(context)
    {
    }
}