using System.Linq.Expressions;
using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using codeTalks.Persistence.Contexts;
using Core.Persistence.Paging;
using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Persistence.Repositories;

public sealed class ChannelRepository(AppDbContext context)
    : EfRepositoryBase<Channel, AppDbContext>(context), IChannelRepository
{
    private readonly AppDbContext _context = context;

    public new async Task<Channel> AddAsync(Channel entity, CancellationToken ct)
    {
        _context.Entry(entity).State = EntityState.Added;

        foreach (var entityChannelUser in entity.ChannelUsers)
        {
            _context.Entry(entityChannelUser).State = EntityState.Added;
        }

        await Context.SaveChangesAsync();
        return entity;
    }

    public async Task<IList<TResult>> GetChannelAdminsAsync<TResult>(Expression<Func<ChannelUser, TResult>> selector,
        string channelId,
        ChannelUserStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ChannelUser>()
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.Role)
            .Where(cu => cu.ChannelId == channelId && cu.Status == status && (cu.Role.Name == "Owner" || cu.Role.Name == "Moderator"))
            .OrderBy(cu => cu.Role.Name == "Owner" ? 0 : 1)
            .ThenBy(cu => cu.User.FirstName)
            .ThenBy(cu => cu.User.MiddleName)
            .ThenBy(cu => cu.User.LastName)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public async Task<IPaginate<TResult>> GetChannelUsersAsync<TResult>(
        Expression<Func<ChannelUser, TResult>> selector,
        string channelId,
        ChannelUserStatus status,
        IReadOnlyCollection<string>? excludeRoleNames = null,
        string? search = null,
        int index = 0,
        int size = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ChannelUser>()
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.Role)
            .Where(cu => cu.ChannelId == channelId && cu.Status == status);

        if (excludeRoleNames is { Count: > 0 })
            query = query.Where(cu => !excludeRoleNames.Contains(cu.Role.Name));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(cu =>
                cu.User.FirstName.ToLower().Contains(term) ||
                (cu.User.MiddleName != null && cu.User.MiddleName.ToLower().Contains(term)) ||
                cu.User.LastName.ToLower().Contains(term));
        }

        if (status is ChannelUserStatus.Accepted or ChannelUserStatus.Banned)
        {
            return await query
                .OrderBy(cu => cu.Role.Name == "Moderator" ? 0 : 1)
                .ThenBy(cu => cu.User.FirstName)
                .ThenBy(cu => cu.User.MiddleName)
                .ThenBy(cu => cu.User.LastName)
                .Select(selector)
                .ToPaginateAsync(index, size, cancellationToken: cancellationToken);
        }
        
        return await query
            .OrderByDescending(cu => cu.CreatedAt)
            .Select(selector)
            .ToPaginateAsync(index, size, cancellationToken: cancellationToken);
    }

    public Task<List<ChannelUser>> GetChannelUsersAsync(
        Expression<Func<ChannelUser, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<ChannelUser>()
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.Role)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }
}