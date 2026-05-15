using codeTalks.Domain;
using codeTalks.Persistance.Contexts;
using Core.Persistence.Repositories;
using Core.Security.Entities;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Persistance.Utilities;

public static class TimestampUpdater
{
    public static void UpdateTimestampsOfEntities(AppDbContext dbContext)
    {
        var baseEntries = dbContext.ChangeTracker.Entries<Entity>();
        var userEntries = dbContext.ChangeTracker.Entries<User>();
        var channelUserEntries = dbContext.ChangeTracker.Entries<ChannelUser>();

        foreach (var entity in baseEntries)
        {
            if (entity.State == EntityState.Added)
            {
                entity.Property(x => x.UpdatedAt).IsModified = false;
                entity.Property(x => x.CreatedAt).CurrentValue = DateTime.UtcNow;
            }
            if (entity.State == EntityState.Modified)
            {
                entity.Property(x => x.CreatedAt).IsModified = false;
                entity.Property(x => x.UpdatedAt).CurrentValue = DateTime.UtcNow;
            }
        }

        foreach (var entity in userEntries)
        {
            if (entity.State == EntityState.Added)
            {
                entity.Property(x => x.UpdatedAt).IsModified = false;
                entity.Property(x => x.CreatedAt).CurrentValue = DateTime.UtcNow;
            }
            if (entity.State == EntityState.Modified)
            {
                entity.Property(x => x.CreatedAt).IsModified = false;
                entity.Property(x => x.UpdatedAt).CurrentValue = DateTime.UtcNow;
            }
        }
        
        foreach (var entity in channelUserEntries)
        {
            if (entity.State == EntityState.Added)
            {
                entity.Property(x => x.UpdatedAt).IsModified = false;
                entity.Property(x => x.CreatedAt).CurrentValue = DateTime.UtcNow;
            }
            if (entity.State == EntityState.Modified)
            {
                entity.Property(x => x.CreatedAt).IsModified = false;
                entity.Property(x => x.UpdatedAt).CurrentValue = DateTime.UtcNow;
            }
        }
    }
}
