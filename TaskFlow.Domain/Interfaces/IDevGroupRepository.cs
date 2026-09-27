using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface IDevGroupRepository : IGenericRepository<DevGroup>
{
    Task<DevGroup?> GetByNameCaseInsensitiveAsync(string name);
    Task<DevGroup?> GetWithMembersAsync(Guid devGroupId);
    Task<IReadOnlyList<DevGroup>> GetActiveOrderedAsync();
}

/// <summary>
/// Membresías de los grupos de desarrollo. No extiende IGenericRepository porque la clave
/// es compuesta (DevGroupId, PersonId).
/// </summary>
public interface IDevGroupMemberRepository
{
    Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid devGroupId);
    Task<IReadOnlyList<Guid>> GetMemberIdsForProjectAsync(Guid projectId);
    Task<bool> IsMemberAsync(Guid devGroupId, Guid personId);
    Task ReplaceMembersAsync(Guid devGroupId, IEnumerable<Guid> personIds);
    Task<int> RemoveByGroupAsync(Guid devGroupId);
}
