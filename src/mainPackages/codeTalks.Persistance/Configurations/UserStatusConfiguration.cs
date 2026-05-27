using codeTalks.Domain;
using Core.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace codeTalks.Persistance.Configurations;

public sealed class UserStatusConfiguration : IEntityTypeConfiguration<UserStatus>
{
    public void Configure(EntityTypeBuilder<UserStatus> builder)
    {
        builder.ToTable("UserStatuses");

        builder.HasOne<User>(us => us.User)
               .WithOne()
               .HasForeignKey<UserStatus>(us => us.UserId);
    }
}