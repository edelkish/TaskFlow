using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class TaskGroupConfiguration : IEntityTypeConfiguration<TaskGroup>
{
    public void Configure(EntityTypeBuilder<TaskGroup> builder)
    {
        builder.HasKey(g => g.Id);

        builder.HasOne(g => g.ImportBatch)
            .WithMany(b => b.TaskGroups)
            .HasForeignKey(g => g.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Period)
            .WithMany(p => p.TaskGroups)
            .HasForeignKey(g => g.PeriodId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(g => g.Project)
            .WithMany(p => p.TaskGroups)
            .HasForeignKey(g => g.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(g => g.DevPerson)
            .WithMany(p => p.DevGroups)
            .HasForeignKey(g => g.DevPersonId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(g => g.TeamLeadPerson)
            .WithMany(p => p.LeadGroups)
            .HasForeignKey(g => g.TeamLeadPersonId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(g => g.QaPerson)
            .WithMany(p => p.QaGroups)
            .HasForeignKey(g => g.QaPersonId)
            .OnDelete(DeleteBehavior.NoAction);

        // Idempotencia al re-importar: bloque Dev por (Periodo, Proyecto, Dev).
        builder.HasIndex(g => new { g.PeriodId, g.ProjectId, g.DevPersonId })
            .IsUnique()
            .HasFilter("[ProjectId] IS NOT NULL AND [DevPersonId] IS NOT NULL");

        // Idempotencia al re-importar: bloque QA-only por (Periodo, QA).
        builder.HasIndex(g => new { g.PeriodId, g.QaPersonId })
            .IsUnique()
            .HasFilter("[ProjectId] IS NULL AND [DevPersonId] IS NULL AND [QaPersonId] IS NOT NULL");
    }
}