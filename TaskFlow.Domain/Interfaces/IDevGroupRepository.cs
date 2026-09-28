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

    /// <summary>
    /// De que grupos son miembros esas personas, para poder rechazar las que ya pertenecen a
    /// uno distinto. Trae el nombre del grupo porque el mensaje de error lo necesita: decir
    /// "ya pertenece a otro grupo" sin decir cual deja al usuario sin saber que hacer.
    /// Se resuelve en una sola consulta con el nombre, en vez de una por persona.
    /// </summary>
    Task<IReadOnlyList<DevGroupOwnership>> GetOwnershipsAsync(
        IEnumerable<Guid> personIds, Guid? excludingGroupId = null);
}

/// <summary>
/// Grupo al que pertenece una persona. No es entidad: es la projection que devuelve
/// <see cref="IDevGroupMemberRepository.GetOwnershipsAsync"/>.
/// </summary>
public record DevGroupOwnership(Guid PersonId, string PersonName, Guid DevGroupId, string DevGroupName);
