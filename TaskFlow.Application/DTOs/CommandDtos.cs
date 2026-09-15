using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record CreateTaskGroupDto
{
    public Guid PeriodId { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid? DevPersonId { get; init; }
    public Guid? TeamLeadPersonId { get; init; }
    public Guid? QaPersonId { get; init; }
}

public record UpdateTaskGroupDto
{
    public Guid? PeriodId { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid? DevPersonId { get; init; }
    public Guid? TeamLeadPersonId { get; init; }
    public Guid? QaPersonId { get; init; }
}

public record CreatePlanningTaskDto
{
    public Guid TaskGroupId { get; init; }
    public int Number { get; init; }
    public int? SubNumber { get; init; }
    public Guid? AssignedPersonId { get; init; }
    public string Description { get; init; } = string.Empty;
}

public record UpdatePlanningTaskDto
{
    public int Number { get; init; }
    public int? SubNumber { get; init; }
    public Guid? AssignedPersonId { get; init; }
    public string Description { get; init; } = string.Empty;
}

public record CreatePeriodDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record CreatePersonDto
{
    public string Name { get; init; } = string.Empty;
    public string? UserId { get; init; }
}

public record UpdatePersonDto
{
    public string Name { get; init; } = string.Empty;
    public string? UserId { get; init; }
}

public record TaskSourceInfo
{
    public TaskSource Source { get; init; }
}