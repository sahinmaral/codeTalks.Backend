using codeTalks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace codeTalks.Persistance.Configurations;

public sealed class ChannelUserConfiguration : IEntityTypeConfiguration<ChannelUser>
{
    public void Configure(EntityTypeBuilder<ChannelUser> builder)
    {
        builder.ToTable("ChannelUsers");
        builder.HasKey(x => new {x.ChannelId, x.UserId});
    }
}