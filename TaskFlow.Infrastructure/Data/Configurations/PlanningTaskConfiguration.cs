using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class PlanningTaskConfiguration : IEntityTypeConfiguration<PlanningTask>
{
    public void Configure(EntityTypeBuilder<PlanningTask> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Description)
            .IsRequired();

        builder.Property(t => t.Source)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(t => t.TaskGroup)
            .WithMany(g => g.PlanningTasks)
            .HasForeignKey(t => t.TaskGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Project)
            .WithMany(p => p.PlanningTasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.AssignedPerson)
            .WithMany(p => p.AssignedTasks)
            .HasForeignKey(t => t.AssignedPersonId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unicidad de tareas padre (Number) y subtareas (Number, SubNumber).
        //
        // El filtro excluye el backlog a propósito. En SQL Server NULL es igual a NULL
        // dentro de un índice único, así que sin el filtro la segunda tarea de backlog
        // (TaskGroupId NULL, Number NULL) violaría la unicidad contra la primera.
        builder.HasIndex(t => new { t.TaskGroupId, t.Number })
            .IsUnique()
            .HasFilter("[TaskGroupId] IS NOT NULL AND [SubNumber] IS NULL");

        builder.HasIndex(t => new { t.TaskGroupId, t.Number, t.SubNumber })
            .IsUnique()
            .HasFilter("[TaskGroupId] IS NOT NULL AND [SubNumber] IS NOT NULL");

        // Sostiene la vista de backlog, que siempre filtra por proyecto.
        builder.HasIndex(t => new { t.ProjectId, t.TaskGroupId });
    }
}
