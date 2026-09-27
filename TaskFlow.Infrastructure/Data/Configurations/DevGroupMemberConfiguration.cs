using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data.Configurations;

public class DevGroupMemberConfiguration : IEntityTypeConfiguration<DevGroupMember>
{
    public void Configure(EntityTypeBuilder<DevGroupMember> builder)
    {
        builder.HasKey(m => new { m.DevGroupId, m.PersonId });

        builder.HasOne(m => m.DevGroup)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.DevGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Person)
            .WithMany(p => p.DevGroupMemberships)
            .HasForeignKey(m => m.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.PersonId);
    }
}
