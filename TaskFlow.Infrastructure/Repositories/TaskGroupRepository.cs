using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskGroupRepository : GenericRepository<TaskGroup>, ITaskGroupRepository
{
    public TaskGroupRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<TaskGroup?> GetByDevBlockKeyAsync(Guid periodId, Guid projectId, Guid devPersonId)
    {
        return await _context.TaskGroups
            .FirstOrDefaultAsync(g => g.PeriodId == periodId
                                      && g.ProjectId == projectId
                                      && g.DevPersonId == devPersonId);
    }

    public async Task<TaskGroup?> GetByQaBlockKeyAsync(Guid periodId, Guid qaPersonId)
    {
        return await _context.TaskGroups
            .FirstOrDefaultAsync(g => g.PeriodId == periodId
                                      && g.ProjectId == null
                                      && g.DevPersonId == null
                                      && g.QaPersonId == qaPersonId);
    }

    public async Task<IEnumerable<TaskGroup>> GetByPeriodAsync(Guid periodId)
    {
        return await _context.TaskGroups
            .Where(g => g.PeriodId == periodId)
            .Include(g => g.Period)
            .Include(g => g.Project)
            .Include(g => g.DevPerson)
            .Include(g => g.TeamLeadPerson)
            .Include(g => g.QaPerson)
            .Include(g => g.PlanningTasks)
                .ThenInclude(t => t.AssignedPerson)
            .OrderBy(g => g.Project != null ? g.Project.Name : string.Empty)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskGroup>> GetByProjectAsync(Guid projectId)
    {
        return await _context.TaskGroups
            .Where(g => g.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<TaskGroup?> GetWithTasksAsync(Guid taskGroupId)
    {
        return await _context.TaskGroups
            .Include(g => g.Project)
            .Include(g => g.DevPerson)
            .Include(g => g.TeamLeadPerson)
            .Include(g => g.QaPerson)
            .Include(g => g.PlanningTasks.OrderBy(t => t.Number).ThenBy(t => t.SubNumber))
            .ThenInclude(t => t.AssignedPerson)
            .FirstOrDefaultAsync(g => g.Id == taskGroupId);
    }
}

public class PlanningTaskRepository : GenericRepository<PlanningTask>, IPlanningTaskRepository
{
    public PlanningTaskRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PlanningTask>> GetByGroupOrderedAsync(Guid taskGroupId)
    {
        return await _context.PlanningTasks
            .Where(t => t.TaskGroupId == taskGroupId)
            .OrderBy(t => t.Number)
            .ThenBy(t => t.SubNumber)
            .Include(t => t.AssignedPerson)
            .ToListAsync();
    }

    public async Task<IEnumerable<PlanningTask>> GetByAssigneeAsync(Guid personId)
    {
        return await _context.PlanningTasks
            .Where(t => t.AssignedPersonId == personId)
            .Include(t => t.TaskGroup)
            .ThenInclude(g => g.Project)
            .OrderByDescending(t => t.TaskGroup.Period.Year)
            .ThenByDescending(t => t.TaskGroup.Period.Month)
            .ToListAsync();
    }

    public async Task<int> DeleteByGroupWhereImportAsync(Guid taskGroupId)
    {
        var imported = await _context.PlanningTasks
            .Where(t => t.TaskGroupId == taskGroupId && t.Source == Domain.Enums.TaskSource.Import)
            .ToListAsync();

        _context.PlanningTasks.RemoveRange(imported);
        return imported.Count;
    }
}

public class ImportBatchRepository : GenericRepository<ImportBatch>, IImportBatchRepository
{
    public ImportBatchRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ImportBatch>> GetOrderedDescAsync()
    {
        return await _context.ImportBatches
            .OrderByDescending(b => b.ImportedAt)
            .ToListAsync();
    }

    public async Task<ImportBatch?> GetWithGroupsAsync(Guid importBatchId)
    {
        return await _context.ImportBatches
            .Include(b => b.Period)
            .Include(b => b.TaskGroups)
            .FirstOrDefaultAsync(b => b.Id == importBatchId);
    }
}