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

    public async Task<Project?> GetProjectWithTasksAsync(Guid projectId)
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<Project?> GetByNameAsync(string name)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<Project?> GetByNameCaseInsensitiveAsync(string name)
    {
        var trimmed = name.Trim();
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Name.ToLower() == trimmed.ToLower());
    }
}
