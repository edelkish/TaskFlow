using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// Registro de una importación de un archivo TXT de tareas.
/// </summary>
public class ImportBatch : BaseEntity
{
    public Guid PeriodId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileEncoding { get; set; } = string.Empty;
    public ImportStatus Status { get; set; } = ImportStatus.Success;
    public int SectionsCount { get; set; }
    public int TasksCount { get; set; }
    public int WarningsCount { get; set; }
    public string? Note { get; set; }
    public DateTime ImportedAt { get; set; }

    // Navigation properties
    public Period Period { get; set; } = null!;
    public ICollection<TaskGroup> TaskGroups { get; set; } = new List<TaskGroup>();
}