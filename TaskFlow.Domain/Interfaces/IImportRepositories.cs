using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface ITaskGroupRepository : IGenericRepository<TaskGroup>
{
    Task<TaskGroup?> GetByDevBlockKeyAsync(Guid periodId, Guid projectId, Guid devPersonId);
    Task<TaskGroup?> GetByQaBlockKeyAsync(Guid periodId, Guid qaPersonId);
    Task<IEnumerable<TaskGroup>> GetByPeriodAsync(Guid periodId);
    Task<IEnumerable<TaskGroup>> GetByProjectAsync(Guid projectId);
    Task<TaskGroup?> GetWithTasksAsync(Guid taskGroupId);
}

public interface IPlanningTaskRepository : IGenericRepository<PlanningTask>
{
    Task<IEnumerable<PlanningTask>> GetByGroupOrderedAsync(Guid taskGroupId);
    Task<IEnumerable<PlanningTask>> GetByAssigneeAsync(Guid personId);
    Task<int> DeleteByGroupWhereImportAsync(Guid taskGroupId);
}

public interface IImportBatchRepository : IGenericRepository<ImportBatch>
{
    Task<IEnumerable<ImportBatch>> GetOrderedDescAsync();
    Task<ImportBatch?> GetWithGroupsAsync(Guid importBatchId);
}