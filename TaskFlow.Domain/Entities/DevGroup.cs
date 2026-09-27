namespace TaskFlow.Domain.Entities;

/// <summary>
/// Grupo de desarrollo: el equipo estable que trabaja en los proyectos. Es distinto de
/// <see cref="TaskGroup"/>, que es un bloque mensual del TXT.
/// </summary>
public class DevGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation properties
    public ICollection<DevGroupMember> Members { get; set; } = new List<DevGroupMember>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
