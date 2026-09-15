using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateTime? DueDate,
    Guid ProjectId,
    string ProjectName,
    Guid? AssignedToId,
    DateTime CreatedAt
);

public record CreateTaskDto(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? DueDate,
    Guid ProjectId,
    Guid? AssignedToId
);

public record UpdateTaskDto(
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateTime? DueDate,
    Guid? AssignedToId
);
