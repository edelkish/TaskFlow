using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// Tarea de planificación. Cubre los dos casos con un solo concepto:
/// <list type="bullet">
/// <item>Tarea de periodo: <see cref="TaskGroupId"/> apunta al bloque mensual del TXT y
/// <see cref="Number"/>/<see cref="SubNumber"/> traen la numeración del archivo.</item>
/// <item>Tarea de backlog: <see cref="TaskGroupId"/> es NULL y cuelga solo de
/// <see cref="ProjectId"/>, sin periodo ni numeración.</item>
/// </list>
/// </summary>
public class PlanningTask : BaseEntity
{
    /// <summary>Bloque mensual del TXT. NULL en tareas de backlog.</summary>
    public Guid? TaskGroupId { get; set; }

    /// <summary>
    /// Proyecto de la tarea. NULL solo en las tareas de período de un bloque QA-only,
    /// que por definición no declara App:. Nunca es NULL en backlog.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>Número de la tarea padre (1..N) dentro del bloque. NULL en backlog.</summary>
    public int? Number { get; set; }

    /// <summary>Subnúmero de la subtarea (N.M); NULL si es tarea padre o de backlog.</summary>
    public int? SubNumber { get; set; }

    /// <summary>Persona asignada (libre); al importar se auto-asigna Dev o QA del bloque.</summary>
    public Guid? AssignedPersonId { get; set; }

    public TaskSource Source { get; set; } = TaskSource.Import;

    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public TaskGroup? TaskGroup { get; set; }
    public Project? Project { get; set; }
    public Person? AssignedPerson { get; set; }
}
