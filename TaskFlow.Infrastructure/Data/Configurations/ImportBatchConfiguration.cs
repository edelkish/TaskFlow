using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(b => b.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.FileEncoding)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.Note)
            .IsRequired(false);

        builder.HasOne(b => b.Period)
            .WithMany(p => p.ImportBatches)
            .HasForeignKey(b => b.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.TaskGroups)
            .WithOne(g => g.ImportBatch)
            .HasForeignKey(g => g.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}