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

        builder.HasOne(t => t.AssignedPerson)
            .WithMany(p => p.AssignedTasks)
            .HasForeignKey(t => t.AssignedPersonId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unicidad de tareas padre (Number) y subtareas (Number, SubNumber).
        builder.HasIndex(t => new { t.TaskGroupId, t.Number })
            .IsUnique()
            .HasFilter("[SubNumber] IS NULL");

        builder.HasIndex(t => new { t.TaskGroupId, t.Number, t.SubNumber })
            .IsUnique()
            .HasFilter("[SubNumber] IS NOT NULL");
    }
}