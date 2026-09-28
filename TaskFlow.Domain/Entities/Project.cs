namespace TaskFlow.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Version { get; set; }
    public int Progress { get; set; } = 0;
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Grupo de desarrollo asignado. Es opcional: no todo proyecto tiene un equipo fijo.
    /// Que el Dev o el QA de un bloque del TXT no pertenezcan a este grupo se avisa como
    /// warning y no bloquea la importacion, porque la planificacion mensual no debe detenerse
    /// por un dato de curacion pendiente. Ojo que es distinto de la regla de asignacion: una
    /// persona no puede pertenecer a dos grupos de desarrollo a la vez, y eso si es un error.
    /// </summary>
    public Guid? DevGroupId { get; set; }

    // Navigation properties
    public DevGroup? DevGroup { get; set; }
    public ICollection<TaskGroup> TaskGroups { get; set; } = new List<TaskGroup>();
    public ICollection<PlanningTask> PlanningTasks { get; set; } = new List<PlanningTask>();
}