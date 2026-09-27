using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface ITaskGroupRepository : IGenericRepository<TaskGroup>
{
    Task<TaskGroup?> GetByDevBlockKeyAsync(Guid periodId, Guid projectId, Guid devPersonId);
    Task<TaskGroup?> GetByQaBlockKeyAsync(Guid periodId, Guid qaPersonId);

    /// <summary>
    /// Clave de idempotencia del bloque que declara App: pero no Dev:. Sin este método ese
    /// bloque nunca se ubicaría al reimportar, porque GetByQaBlockKeyAsync exige ProjectId
    /// NULL y este bloque sí tiene proyecto.
    /// </summary>
    Task<TaskGroup?> GetByQaWithProjectBlockKeyAsync(Guid periodId, Guid projectId, Guid qaPersonId);
    Task<IEnumerable<TaskGroup>> GetByPeriodAsync(Guid periodId);
    Task<IEnumerable<TaskGroup>> GetByProjectAsync(Guid projectId);
    Task<TaskGroup?> GetWithTasksAsync(Guid taskGroupId);
}

public interface IPlanningTaskRepository : IGenericRepository<PlanningTask>
{
    Task<IEnumerable<PlanningTask>> GetByGroupOrderedAsync(Guid taskGroupId);

    /// <summary>Tareas de backlog: sin grupo, colgando del proyecto.</summary>
    Task<IEnumerable<PlanningTask>> GetBacklogAsync(Guid? projectId = null, Guid? assigneeId = null);

    Task<IEnumerable<PlanningTask>> GetByAssigneeAsync(Guid personId);
    Task<int> DeleteByGroupWhereImportAsync(Guid taskGroupId);
}

public interface IImportBatchRepository : IGenericRepository<ImportBatch>
{
    Task<IEnumerable<ImportBatch>> GetOrderedDescAsync();
    Task<ImportBatch?> GetWithGroupsAsync(Guid importBatchId);
}