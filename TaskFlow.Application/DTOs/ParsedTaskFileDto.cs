namespace TaskFlow.Application.DTOs;

public record ParsedTaskDto
{
    public int Number { get; set; }
    public int? SubNumber { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Línea 1-based donde se declara la tarea.</summary>
    public int Line { get; set; }
}

public record ParsedTaskGroupDto
{
    public string? ProjectName { get; set; }
    public string? DevName { get; set; }
    public string TeamLeadName { get; set; } = string.Empty;
    public string QaName { get; set; } = string.Empty;
    public bool IsQaOnly => string.IsNullOrWhiteSpace(ProjectName) && string.IsNullOrWhiteSpace(DevName);
    public List<ParsedTaskDto> Tasks { get; set; } = new();

    /// <summary>Índice 1-based del bloque dentro del archivo.</summary>
    public int Index { get; set; }

    /// <summary>Línea 1-based donde arranca el bloque.</summary>
    public int StartLine { get; set; }

    // Línea de cada etiqueta, para que un error apunte a la línea exacta y no al bloque.
    public int ProjectLine { get; set; }
    public int DevLine { get; set; }
    public int TeamLeadLine { get; set; }
    public int QaLine { get; set; }

    public int LineOfField(string field) => field switch
    {
        "App" => ProjectLine,
        "Dev" => DevLine,
        "Team Lead" => TeamLeadLine,
        "QA" => QaLine,
        _ => StartLine
    };
}

public record ParsedTaskFileDto
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileEncoding { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public List<ParsedTaskGroupDto> Groups { get; set; } = new();

    /// <summary>
    /// Problemas detectados al leer el archivo. El parser es permisivo: registra y
    /// sigue, para que la validación pueda reportar todos los errores juntos en vez de
    /// fallar en el primero.
    /// </summary>
    public List<ImportFindingDto> Findings { get; set; } = new();

    /// <summary>Compatibilidad con las vistas que solo mostraban strings.</summary>
    public List<string> Warnings => Findings.Select(f => f.Message).ToList();
}
