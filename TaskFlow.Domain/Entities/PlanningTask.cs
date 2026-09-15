using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// Tarea de planificación: padre (SubNumber NULL) o subtarea (Number + SubNumber).
/// </summary>
public class PlanningTask : BaseEntity
{
    public Guid TaskGroupId { get; set; }

    /// <summary>Número de la tarea padre (1..N).</summary>
    public int Number { get; set; }

    /// <summary>Subnúmero de la subtarea (N.M); NULL si es tarea padre.</summary>
    public int? SubNumber { get; set; }

    /// <summary>Persona asignada (libre); al importar se auto-asigna Dev o QA del bloque.</summary>
    public Guid? AssignedPersonId { get; set; }

    public TaskSource Source { get; set; } = TaskSource.Import;

    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public TaskGroup TaskGroup { get; set; } = null!;
    public Person? AssignedPerson { get; set; }
}