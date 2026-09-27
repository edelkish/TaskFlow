using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        var trimmed = name.Trim();
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == trimmed);
    }

    public async Task<Role?> GetByNameCaseInsensitiveAsync(string name)
    {
        // Roles.Name usa la collation Latin1_General_CI_AI.
        var trimmed = name.Trim();
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == trimmed);
    }

    public async Task<IReadOnlyList<Role>> GetActiveOrderedAsync()
    {
        return await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }
}

public class PersonRoleRepository : IPersonRoleRepository
{
    private readonly TaskFlowDbContext _context;

    public PersonRoleRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Role>> GetRolesByPersonAsync(Guid personId)
    {
        return await _context.PersonRoles
            .Where(pr => pr.PersonId == personId)
            .Select(pr => pr.Role)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetRoleIdsByPersonAsync(Guid personId)
    {
        return await _context.PersonRoles
            .Where(pr => pr.PersonId == personId)
            .Select(pr => pr.RoleId)
            .ToListAsync();
    }

    public async Task<bool> HasRoleAsync(Guid personId, Guid roleId)
    {
        return await _context.PersonRoles
            .AnyAsync(pr => pr.PersonId == personId && pr.RoleId == roleId);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetPersonCountsByRoleAsync()
    {
        var rows = await _context.PersonRoles
            .GroupBy(pr => pr.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToListAsync();

        return rows.ToDictionary(r => r.RoleId, r => r.Count);
    }

    public async Task AddAsync(PersonRole personRole)
    {
        await _context.PersonRoles.AddAsync(personRole);
    }

    public async Task RemoveAsync(Guid personId, Guid roleId)
    {
        var existing = await _context.PersonRoles
            .FirstOrDefaultAsync(pr => pr.PersonId == personId && pr.RoleId == roleId);

        if (existing != null)
        {
            _context.PersonRoles.Remove(existing);
        }

        await Task.CompletedTask;
    }

    public async Task RemoveByRoleAsync(Guid roleId)
    {
        var existing = await _context.PersonRoles
            .Where(pr => pr.RoleId == roleId)
            .ToListAsync();

        _context.PersonRoles.RemoveRange(existing);
        await Task.CompletedTask;
    }

    public async Task<int> RemoveByPersonAsync(Guid personId)
    {
        var existing = await _context.PersonRoles
            .Where(pr => pr.PersonId == personId)
            .ToListAsync();

        _context.PersonRoles.RemoveRange(existing);
        return existing.Count;
    }
}
