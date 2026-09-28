using TaskFlow.Application.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record PeriodDto
{
    public Guid Id { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record PersonDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LastName { get; init; }
    public string? UserName { get; init; }
    public string? UserId { get; init; }

    /// <summary>Nombre y apellidos juntos, para mostrarlos en listas y selectores.</summary>
    public string FullName => PersonDisplayName.For(Name, LastName);

    public List<string> Roles { get; init; } = new();
    public List<Guid> RoleIds { get; init; } = new();
}

public record ImportBatchDto
{
    public Guid Id { get; init; }
    public string PeriodName { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FileEncoding { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int SectionsCount { get; init; }
    public int TasksCount { get; init; }
    public int WarningsCount { get; init; }
    public string? Note { get; init; }
    public DateTime ImportedAt { get; init; }
}

public record PlanningTaskDto
{
    public Guid Id { get; init; }

    /// <summary>NULL en tareas de backlog.</summary>
    public Guid? TaskGroupId { get; init; }

    /// <summary>NULL en las tareas de período de un bloque QA-only.</summary>
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }

    /// <summary>Naming del TXT. NULL en tareas de backlog.</summary>
    public int? Number { get; init; }
    public int? SubNumber { get; init; }

    public Guid? AssignedPersonId { get; init; }
    public string? AssignedPersonName { get; init; }
    public TaskSource Source { get; init; }
    public string Description { get; init; } = string.Empty;

    public bool IsBacklog => TaskGroupId == null;
}

public record TaskGroupDto
{
    public Guid Id { get; init; }
    public Guid PeriodId { get; init; }
    public string PeriodName { get; init; } = string.Empty;
    public Guid? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public Guid? DevPersonId { get; init; }
    public string? DevName { get; init; }
    public Guid? TeamLeadPersonId { get; init; }
    public string? TeamLeadName { get; init; }
    public Guid? QaPersonId { get; init; }
    public string? QaName { get; init; }
    public bool IsQaOnly => ProjectId == null && DevPersonId == null;
    public List<PlanningTaskDto> Tasks { get; init; } = new();
}

public record ImportResultDto
{
    public Guid ImportBatchId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int GroupsCreated { get; set; }
    public int GroupsUpdated { get; set; }
    public int TasksImported { get; set; }
    public int TasksRemoved { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public record DevGroupMemberDto
{
    public Guid PersonId { get; init; }
    public string PersonName { get; init; } = string.Empty;
}

public record DevGroupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int MemberCount { get; init; }
    public int ProjectCount { get; init; }
    public List<DevGroupMemberDto> Members { get; init; } = new();
}

public record CreateDevGroupDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<Guid> MemberIds { get; init; } = new();
}

public record UpdateDevGroupDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record SetDevGroupMembersDto
{
    public List<Guid> PersonIds { get; init; } = new();
}