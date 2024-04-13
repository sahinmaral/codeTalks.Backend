using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistance.Contexts;
using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Persistance.Repositories;

public sealed class ChannelRepository :  EfRepositoryBase<Channel, AppDbContext>, IChannelRepository
{
    private readonly AppDbContext _context;

    public ChannelRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

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