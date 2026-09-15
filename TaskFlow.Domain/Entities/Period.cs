namespace TaskFlow.Domain.Entities;

/// <summary>
/// Periodo mensual de planificación (ej. "Agosto 2026").
/// </summary>
public class Period : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();
    public ICollection<TaskGroup> TaskGroups { get; set; } = new List<TaskGroup>();
}