namespace TaskFlow.Application.DTOs;

public record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int PersonCount { get; init; }
}

public record CreateRoleDto
{
    public string Name { get; init; } = string.Empty;
}

public record UpdateRoleDto
{
    public string Name { get; init; } = string.Empty;
}

public record SetPersonRolesDto
{
    public List<Guid> RoleIds { get; init; } = new();
}
