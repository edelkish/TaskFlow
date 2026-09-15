namespace TaskFlow.Domain.Entities;

/// <summary>
/// Bloque del TXT: asignación mensual de una persona (Dev o QA-only) a un proyecto.
/// </summary>
public class TaskGroup : BaseEntity
{
    public Guid ImportBatchId { get; set; }
    public Guid PeriodId { get; set; }

    /// <summary>NULL en bloques QA-only.</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>NULL en bloques QA-only.</summary>
    public Guid? DevPersonId { get; set; }

    public Guid? TeamLeadPersonId { get; set; }
    public Guid? QaPersonId { get; set; }

    // Navigation properties
    public ImportBatch ImportBatch { get; set; } = null!;
    public Period Period { get; set; } = null!;
    public Project? Project { get; set; }
    public Person? DevPerson { get; set; }
    public Person? TeamLeadPerson { get; set; }
    public Person? QaPerson { get; set; }
    public ICollection<PlanningTask> PlanningTasks { get; set; } = new List<PlanningTask>();
}