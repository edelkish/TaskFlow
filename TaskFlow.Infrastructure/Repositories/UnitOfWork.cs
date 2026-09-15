using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly Data.TaskFlowDbContext _context;
    private IProjectRepository? _projects;
    private ITaskRepository? _tasks;
    private IPeriodRepository? _periods;
    private IPersonRepository? _people;
    private ITaskGroupRepository? _taskGroups;
    private IPlanningTaskRepository? _planningTasks;
    private IImportBatchRepository? _importBatches;

    public UnitOfWork(Data.TaskFlowDbContext context)
    {
        _context = context;
    }

    public IProjectRepository Projects =>
        _projects ??= new ProjectRepository(_context);

    public ITaskRepository Tasks =>
        _tasks ??= new TaskRepository(_context);

    public IPeriodRepository Periods =>
        _periods ??= new PeriodRepository(_context);

    public IPersonRepository People =>
        _people ??= new PersonRepository(_context);

    public ITaskGroupRepository TaskGroups =>
        _taskGroups ??= new TaskGroupRepository(_context);

    public IPlanningTaskRepository PlanningTasks =>
        _planningTasks ??= new PlanningTaskRepository(_context);

    public IImportBatchRepository ImportBatches =>
        _importBatches ??= new ImportBatchRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}