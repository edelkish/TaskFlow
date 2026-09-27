using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface IRoleRepository : IGenericRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);
    Task<Role?> GetByNameCaseInsensitiveAsync(string name);
    Task<IReadOnlyList<Role>> GetActiveOrderedAsync();
}

/// <summary>
/// Tabla puente Persona-Role. No extiende <see cref="IGenericRepository{T}"/> porque
/// no tiene identidad propia: su clave es compuesta (PersonId, RoleId).
/// </summary>
public interface IPersonRoleRepository
{
    Task<IReadOnlyList<Role>> GetRolesByPersonAsync(Guid personId);
    Task<IReadOnlyList<Guid>> GetRoleIdsByPersonAsync(Guid personId);
    Task<bool> HasRoleAsync(Guid personId, Guid roleId);
    Task<IReadOnlyDictionary<Guid, int>> GetPersonCountsByRoleAsync();
    Task AddAsync(PersonRole personRole);
    Task RemoveAsync(Guid personId, Guid roleId);
    Task RemoveByRoleAsync(Guid roleId);
    Task<int> RemoveByPersonAsync(Guid personId);
}
