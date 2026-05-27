using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistance.Contexts;
using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Persistance.Repositories;

public sealed class ChannelRepository(AppDbContext context)
    : EfRepositoryBase<Channel, AppDbContext>(context), IChannelRepository
{
    private readonly AppDbContext _context = context;

    public new async Task<Channel> AddAsync(Channel entity)
    {
        _context.Entry(entity).State = EntityState.Added;
        
        foreach (var entityChannelUser in entity.ChannelUsers)
        {
            _context.Entry(entityChannelUser).State = EntityState.Added;
        }
        
        await Context.SaveChangesAsync();
        return entity;
    }
}