using Core.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace codeTalks.Persistance.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);

        builder.HasData(
            new Role {Id = "ec128130-96b8-4fa9-b624-a7fd8bf9c5d2", Name = "User", NormalizedName = "USER", ConcurrencyStamp = "d3a9a937-e0a2-401c-9cca-b95403e44cf8"});
        
        builder.HasData(
            new Role {Id = "b1c487ef-1fa4-4f96-a8ab-b6cb14216a86", Name = "Moderator", NormalizedName = "MODERATOR", ConcurrencyStamp = "f370f307-6959-4896-ae23-0ea826a00261"});
        
        builder.HasData(
            new Role {Id = "ea552ce1-296c-4607-b7e1-9f0d21de9499", Name = "Owner", NormalizedName = "OWNER", ConcurrencyStamp = "e633761d-0a88-4b95-bc75-bb19efff08fb"});
    }
}
