using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class PeriodConfiguration : IEntityTypeConfiguration<Period>
{
    public void Configure(EntityTypeBuilder<Period> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(p => p.Year)
            .IsRequired();

        builder.Property(p => p.Month)
            .IsRequired();

        builder.HasIndex(p => new { p.Year, p.Month })
            .IsUnique();
    }
}