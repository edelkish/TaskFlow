namespace TaskFlow.Application.DTOs;

public record ProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Version { get; init; }
    public int Progress { get; init; }
    public Guid OwnerId { get; init; }
    public DateTime CreatedAt { get; init; }

    public Guid? DevGroupId { get; init; }
    public string? DevGroupName { get; init; }

    /// <summary>Tareas de backlog del proyecto (sin grupo).</summary>
    public int TaskCount { get; init; }
}

public record CreateProjectDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Version { get; init; }
    public int Progress { get; init; } = 0;
    public Guid? DevGroupId { get; init; }
}

public record UpdateProjectDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Version { get; init; }
    public int Progress { get; init; }
    public Guid? DevGroupId { get; init; }
}
