using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime? EndDate,
    Guid OwnerId,
    DateTime CreatedAt,
    int TaskCount
);

public record CreateProjectDto(
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime? EndDate
);

public record UpdateProjectDto(
    string Name,
    string? Description,
    DateTime? EndDate
);
