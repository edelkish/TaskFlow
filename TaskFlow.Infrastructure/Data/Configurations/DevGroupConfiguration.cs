using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class DevGroupConfiguration : IEntityTypeConfiguration<DevGroup>
{
    public void Configure(EntityTypeBuilder<DevGroup> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100)
            .UseCollation("Latin1_General_CI_AI");

        builder.Property(g => g.Description)
            .HasMaxLength(500);

        builder.HasIndex(g => g.Name)
            .IsUnique();
    }
}
