namespace TaskFlow.Application.DTOs;

public record ParsedTaskDto
{
    public int Number { get; set; }
    public int? SubNumber { get; set; }
    public string Description { get; set; } = string.Empty;
}

public record ParsedTaskGroupDto
{
    public string? ProjectName { get; set; }
    public string? DevName { get; set; }
    public string TeamLeadName { get; set; } = string.Empty;
    public string QaName { get; set; } = string.Empty;
    public bool IsQaOnly => string.IsNullOrWhiteSpace(ProjectName) && string.IsNullOrWhiteSpace(DevName);
    public List<ParsedTaskDto> Tasks { get; set; } = new();
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
    public List<string> Warnings { get; set; } = new();
}