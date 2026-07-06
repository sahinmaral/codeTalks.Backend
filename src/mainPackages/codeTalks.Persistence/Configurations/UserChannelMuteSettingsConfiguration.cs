using codeTalks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace codeTalks.Persistence.Configurations;

public sealed class UserChannelMuteSettingsConfiguration : IEntityTypeConfiguration<UserChannelMuteSetting>
{
    public void Configure(EntityTypeBuilder<UserChannelMuteSetting> builder)
    {
        builder.ToTable("UserChannelMuteSettings");
    }
}