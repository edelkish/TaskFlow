namespace TaskFlow.Application.DTOs;

public enum ImportFindingSeverity
{
    Warning = 0,
    Error = 1
}

/// <summary>
/// Catálogo cerrado de los problemas que puede reportar la validación. El código es
/// estable para que el cliente pueda reaccionar por código y no por texto.
/// </summary>
public enum ImportFindingCode
{
    FileEmpty,
    PeriodNotParsed,
    MonthUnknown,
    PeriodNotFound,
    ProjectNotFound,
    ProjectInactive,

    /// <summary>
    /// El bloque no trae línea App:. Desde la Fase 5 la tarea cuelga del proyecto
    /// (PlanningTask.ProjectId es required), así que un bloque sin proyecto no se puede
    /// persistir aunque el resto del bloque sea válido.
    /// </summary>
    ProjectRequired,

    PersonNotFound,
    PersonInactive,
    MissingRole,

    /// <summary>
    /// El Dev o el QA del bloque no pertenece al grupo de desarrollo del proyecto.
    /// Warning, no error: la pertenencia se está depurando y no debe frenar la
    /// planificación mensual.
    /// </summary>
    PersonNotInDevGroup,

    BlockWithoutTasks,
    BlockWithoutOwner,
    UnrecognizedLine,
    IgnoredLine
}

public record ImportFindingDto
{
    public ImportFindingCode Code { get; init; }
    public ImportFindingSeverity Severity { get; init; }

    /// <summary>Línea 1-based del archivo donde se origina el problema. 0 si es global.</summary>
    public int Line { get; init; }

    /// <summary>Índice 1-based del bloque. 0 si el problema es global.</summary>
    public int BlockIndex { get; init; }

    /// <summary>Campo afectado: App, Dev, Team Lead, QA o Tasks.</summary>
    public string? Field { get; init; }

    /// <summary>Valor tal como aparece en el archivo, sin normalizar.</summary>
    public string? Value { get; init; }

    public string Message { get; init; } = string.Empty;

    /// <summary>Acción concreta que el usuario debe tomar para resolverlo.</summary>
    public string? Resolution { get; init; }
}

public record ImportBlockPreviewDto
{
    public int Index { get; init; }
    public int StartLine { get; init; }
    public string? ProjectName { get; init; }
    public string? DevName { get; init; }
    public string? TeamLeadName { get; init; }
    public string? QaName { get; init; }
    public bool IsQaOnly { get; init; }
    public int TasksCount { get; init; }
    public bool IsValid { get; init; }
    public List<ImportFindingDto> Findings { get; init; } = new();
}

public record ImportValidationDto
{
    /// <summary>Solo hay un modo de importar: todo o nada. No existe importación parcial.</summary>
    public bool IsValid { get; init; }

    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Ruta absoluta del archivo en el servidor. Se expone porque el lote de auditoría de un
    /// rechazo la necesita: ImportBatches.FilePath es NOT NULL, así que sin esto el registro
    /// del intento fallido no se podría escribir.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    public string FileEncoding { get; init; } = string.Empty;
    public string PeriodName { get; init; } = string.Empty;
    public int? Month { get; init; }
    public int? Year { get; init; }
    public Guid? PeriodId { get; init; }

    public int BlocksCount { get; init; }
    public int TasksCount { get; init; }
    public int ErrorsCount { get; init; }
    public int WarningsCount { get; init; }

    public List<ImportFindingDto> Findings { get; init; } = new();
    public List<ImportBlockPreviewDto> Blocks { get; init; } = new();

    public string Summary => IsValid
        ? $"Archivo válido: {BlocksCount} bloque(s), {TasksCount} tarea(s)."
        : $"Importación rechazada: {ErrorsCount} error(es) y {WarningsCount} advertencia(s). " +
          "No se modificó ningún dato.";
}

/// <summary>
/// Resultado de ejecutar una importación. El servicio no conoce HTTP: devuelve esto
/// y el controlador decide entre 200 y 409.
/// </summary>
public record ImportExecutionDto
{
    public bool Accepted { get; init; }

    /// <summary>Siempre presente: sirve tanto de previsualización como de motivo del rechazo.</summary>
    public ImportValidationDto Validation { get; init; } = new();

    public ImportResultDto? Result { get; init; }

    /// <summary>Lote Failed registrado para auditar el intento rechazado, si se llegó a crearlo.</summary>
    public Guid? RejectedBatchId { get; init; }
}

/// <summary>Cuerpo de la respuesta 409 cuando la importación es rechazada.</summary>
public record ImportRejectionDto
{
    public string Message { get; init; } = string.Empty;
    public ImportValidationDto Validation { get; init; } = new();
    public Guid? RejectedBatchId { get; init; }
}

/// <summary>Resultado de intentar importar, sea aceptado o rechazado.</summary>
public record ImportAttemptDto
{
    public bool Accepted => Rejection == null;
    public ImportResultDto? Result { get; init; }
    public ImportRejectionDto? Rejection { get; init; }
}
