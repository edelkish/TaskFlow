using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    public ProjectRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Project>> GetProjectsByOwnerAsync(Guid ownerId)
    {
        return await _context.Projects
            .Where(p => p.OwnerId == ownerId && p.IsActive)
            .ToListAsync();
    }

    /// <summary>
    /// Proyecto con su grupo de desarrollo y su backlog. Las tareas de periodo cuelgan
    /// del TaskGroup, no del proyecto, así que solo se cargan las de backlog aquí.
    /// </summary>
    public async Task<Project?> GetProjectWithTasksAsync(Guid projectId)
    {
        return await _context.Projects
            .Include(p => p.DevGroup)
            .Include(p => p.PlanningTasks.Where(t => t.TaskGroupId == null))
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<Project?> GetByNameAsync(string name)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<Project?> GetByNameCaseInsensitiveAsync(string name)
    {
        // Projects.Name usa la collation Latin1_General_CI_AI, así que la comparación
        // directa ya es insensible a mayúsculas y acentos y usa el índice único.
        var trimmed = name.Trim();
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Name == trimmed);
    }
}
