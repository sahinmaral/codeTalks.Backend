using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using Core.Persistence.Repositories;

namespace codeTalks.Persistence.Repositories;

public sealed class MessageRepository(AppDbContext context)
    : EfRepositoryBase<Message, AppDbContext>(context), IMessageRepository;