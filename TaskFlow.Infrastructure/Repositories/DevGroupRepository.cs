using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class DevGroupRepository : GenericRepository<DevGroup>, IDevGroupRepository
{
    public DevGroupRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<DevGroup?> GetByNameCaseInsensitiveAsync(string name)
    {
        // DevGroups.Name usa la collation Latin1_General_CI_AI.
        var trimmed = name.Trim();
        return await _context.DevGroups
            .FirstOrDefaultAsync(g => g.Name == trimmed);
    }

    public async Task<DevGroup?> GetWithMembersAsync(Guid devGroupId)
    {
        return await _context.DevGroups
            .Include(g => g.Members)
                .ThenInclude(m => m.Person)
            .FirstOrDefaultAsync(g => g.Id == devGroupId);
    }

    public async Task<IReadOnlyList<DevGroup>> GetActiveOrderedAsync()
    {
        return await _context.DevGroups
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }
}

public class DevGroupMemberRepository : IDevGroupMemberRepository
{
    private readonly TaskFlowDbContext _context;

    public DevGroupMemberRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid devGroupId)
    {
        return await _context.DevGroupMembers
            .Where(m => m.DevGroupId == devGroupId)
            .Select(m => m.PersonId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetMemberIdsForProjectAsync(Guid projectId)
    {
        var devGroupId = await _context.Projects
            .Where(p => p.Id == projectId)
            .Select(p => p.DevGroupId)
            .FirstOrDefaultAsync();

        if (devGroupId == null)
        {
            return Array.Empty<Guid>();
        }

        return await GetMemberIdsAsync(devGroupId.Value);
    }

    public async Task<bool> IsMemberAsync(Guid devGroupId, Guid personId)
    {
        return await _context.DevGroupMembers
            .AnyAsync(m => m.DevGroupId == devGroupId && m.PersonId == personId);
    }

    public async Task ReplaceMembersAsync(Guid devGroupId, IEnumerable<Guid> personIds)
    {
        var requested = personIds.Distinct().ToList();

        var current = await _context.DevGroupMembers
            .Where(m => m.DevGroupId == devGroupId)
            .ToListAsync();

        var currentIds = current.Select(m => m.PersonId).ToHashSet();

        _context.DevGroupMembers.RemoveRange(current.Where(m => !requested.Contains(m.PersonId)));

        foreach (var personId in requested.Where(p => !currentIds.Contains(p)))
        {
            await _context.DevGroupMembers.AddAsync(new DevGroupMember
            {
                DevGroupId = devGroupId,
                PersonId = personId
            });
        }
    }

    public async Task<int> RemoveByGroupAsync(Guid devGroupId)
    {
        var current = await _context.DevGroupMembers
            .Where(m => m.DevGroupId == devGroupId)
            .ToListAsync();

        _context.DevGroupMembers.RemoveRange(current);
        return current.Count;
    }

    public async Task<IReadOnlyList<DevGroupOwnership>> GetOwnershipsAsync(
        IEnumerable<Guid> personIds, Guid? excludingGroupId = null)
    {
        var requested = personIds.Distinct().ToList();
        if (requested.Count == 0)
        {
            return Array.Empty<DevGroupOwnership>();
        }

        // Se proyecta solo lo plano: PersonDisplayName.For es un metodo estatico de C# y EF
        // no podria traducirlo a SQL, asi que el nombre completo se compone en memoria
        // despues de traer las filas. Los alias son obligatorios porque el grupo y la
        // persona tienen ambos una columna Name.
        var rows = await (
                from m in _context.DevGroupMembers
                join g in _context.DevGroups on m.DevGroupId equals g.Id
                join p in _context.People on m.PersonId equals p.Id
                where requested.Contains(m.PersonId)
                      && (excludingGroupId == null || m.DevGroupId != excludingGroupId)
                select new
                {
                    m.PersonId,
                    DevGroupId = g.Id,
                    DevGroupName = g.Name,
                    PersonFirstName = p.Name,
                    PersonLastName = p.LastName
                })
            .ToListAsync();

        return rows
            .Select(r => new DevGroupOwnership(
                r.PersonId,
                PersonDisplayName.For(r.PersonFirstName, r.PersonLastName),
                r.DevGroupId,
                r.DevGroupName))
            .ToList();
    }
}
