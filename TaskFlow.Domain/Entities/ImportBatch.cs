using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// Registro de una importación de un archivo TXT de tareas.
/// </summary>
public class ImportBatch : BaseEntity
{
    /// <summary>
    /// Nullable a propósito: un intento rechazado por un periodo inexistente debe poder
    /// auditarse igual. Si fuera obligatorio, el rechazo más común ni siquiera se podría
    /// registrar.
    /// </summary>
    public Guid? PeriodId { get; set; }

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
    public Period? Period { get; set; }
    public ICollection<TaskGroup> TaskGroups { get; set; } = new List<TaskGroup>();
}