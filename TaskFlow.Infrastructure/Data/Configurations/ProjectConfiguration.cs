using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        // Misma collation que People.Name para que la comparación por nombre sea
        // consistente entre proyectos y personas al validar el TXT.
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200)
            .UseCollation("Latin1_General_CI_AI");

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Version)
            .HasMaxLength(50);

        builder.Property(p => p.Progress)
            .IsRequired()
            .HasDefaultValue(0);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Projects_Progress_Range", "[Progress] >= 0 AND [Progress] <= 100"));

        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.HasOne(p => p.DevGroup)
            .WithMany(g => g.Projects)
            .HasForeignKey(p => p.DevGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.PlanningTasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}