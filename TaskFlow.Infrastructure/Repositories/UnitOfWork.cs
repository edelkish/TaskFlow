using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly Data.TaskFlowDbContext _context;
    private IProjectRepository? _projects;
    private IPeriodRepository? _periods;
    private IPersonRepository? _people;
    private IRoleRepository? _roles;
    private IPersonRoleRepository? _personRoles;
    private IDevGroupRepository? _devGroups;
    private IDevGroupMemberRepository? _devGroupMembers;
    private ITaskGroupRepository? _taskGroups;
    private IPlanningTaskRepository? _planningTasks;
    private IImportBatchRepository? _importBatches;
    private IAppSettingRepository? _appSettings;

    public UnitOfWork(Data.TaskFlowDbContext context)
    {
        _context = context;
    }

    public IProjectRepository Projects =>
        _projects ??= new ProjectRepository(_context);

    public IPeriodRepository Periods =>
        _periods ??= new PeriodRepository(_context);

    public IPersonRepository People =>
        _people ??= new PersonRepository(_context);

    public IRoleRepository Roles =>
        _roles ??= new RoleRepository(_context);

    public IPersonRoleRepository PersonRoles =>
        _personRoles ??= new PersonRoleRepository(_context);

    public IDevGroupRepository DevGroups =>
        _devGroups ??= new DevGroupRepository(_context);

    public IDevGroupMemberRepository DevGroupMembers =>
        _devGroupMembers ??= new DevGroupMemberRepository(_context);

    public ITaskGroupRepository TaskGroups =>
        _taskGroups ??= new TaskGroupRepository(_context);

    public IPlanningTaskRepository PlanningTasks =>
        _planningTasks ??= new PlanningTaskRepository(_context);

    public IImportBatchRepository ImportBatches =>
        _importBatches ??= new ImportBatchRepository(_context);

    public IAppSettingRepository AppSettings =>
        _appSettings ??= new AppSettingRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

internal sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
{
    private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _transaction;

    public EfUnitOfWorkTransaction(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}