namespace TaskFlow.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProjectRepository Projects { get; }
    ITaskRepository Tasks { get; }
    IPeriodRepository Periods { get; }
    IPersonRepository People { get; }
    ITaskGroupRepository TaskGroups { get; }
    IPlanningTaskRepository PlanningTasks { get; }
    IImportBatchRepository ImportBatches { get; }
    IAppSettingRepository AppSettings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}