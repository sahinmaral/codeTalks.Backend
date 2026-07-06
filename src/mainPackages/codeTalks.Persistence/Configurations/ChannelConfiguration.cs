using codeTalks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace codeTalks.Persistence.Configurations;

public sealed class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("Channels");
        builder.HasKey(x => x.Id);
        
        builder
            .HasIndex(c => c.InviteCode)
            .IsUnique();

        builder
            .Property(c => c.InviteCode)
            .HasMaxLength(10)
            .IsRequired();
        
        builder
            .Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder
            .Property(c => c.JoinPolicy)
            .HasDefaultValue(ChannelJoinPolicy.Request);
        
        builder
            .HasQueryFilter(c => c.IsActive);
    }
}