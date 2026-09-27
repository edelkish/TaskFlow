using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(30)
            .UseCollation("Latin1_General_CI_AI");

        builder.HasIndex(r => r.Name)
            .IsUnique();

        // Los tres cargos que el TXT puede exigir. Se siembran como datos de la
        // migración (auditable y reversible) en vez de solo en el seeder de arranque.
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new Role { Id = DevRoleId, Name = CatalogSeeder.DevCargo, IsActive = true, CreatedAt = seededAt },
            new Role { Id = TeamLeadRoleId, Name = CatalogSeeder.TeamLeadCargo, IsActive = true, CreatedAt = seededAt },
            new Role { Id = QaRoleId, Name = CatalogSeeder.QaCargo, IsActive = true, CreatedAt = seededAt });
    }

    public static readonly Guid DevRoleId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TeamLeadRoleId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid QaRoleId = new("33333333-3333-3333-3333-333333333333");
}
