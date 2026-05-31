using codeTalks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace codeTalks.Persistance.Configurations;

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
    }
}