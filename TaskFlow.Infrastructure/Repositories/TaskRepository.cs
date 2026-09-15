using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskRepository : GenericRepository<TaskItem>, ITaskRepository
{
    public TaskRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByProjectAsync(Guid projectId)
    {
        return await _context.Tasks
            .Where(t => t.ProjectId == projectId && t.IsActive)
            .Include(t => t.Project)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByStatusAsync(TaskItemStatus status)
    {
        return await _context.Tasks
            .Where(t => t.Status == status && t.IsActive)
            .Include(t => t.Project)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetTasksByAssigneeAsync(Guid assigneeId)
    {
        return await _context.Tasks
            .Where(t => t.AssignedToId == assigneeId && t.IsActive)
            .Include(t => t.Project)
            .ToListAsync();
    }
}
