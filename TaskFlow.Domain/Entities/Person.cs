namespace TaskFlow.Domain.Entities;

/// <summary>
/// Persona del grupo de desarrollo (Dev, TeamLead o QA) normalizada.
/// </summary>
public class Person : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Identificador del usuario Identity vinculado (AspNetUsers.Id).</summary>
    public string? UserId { get; set; }

    // Navigation properties
    public ICollection<TaskGroup> DevGroups { get; set; } = new List<TaskGroup>();
    public ICollection<TaskGroup> LeadGroups { get; set; } = new List<TaskGroup>();
    public ICollection<TaskGroup> QaGroups { get; set; } = new List<TaskGroup>();
    public ICollection<PlanningTask> AssignedTasks { get; set; } = new List<PlanningTask>();
}