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
    /// Grupo de desarrollo asignado. Es opcional: no todo proyecto tiene un equipo fijo,
    /// y la pertenencia de los Dev del TXT al grupo se valida como advertencia, no como
    /// error, precisamente porque hoy los datos no están limpios.
    /// </summary>
    public Guid? DevGroupId { get; set; }

    // Navigation properties
    public DevGroup? DevGroup { get; set; }
    public ICollection<TaskGroup> TaskGroups { get; set; } = new List<TaskGroup>();
    public ICollection<PlanningTask> PlanningTasks { get; set; } = new List<PlanningTask>();
}