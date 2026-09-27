namespace TaskFlow.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProjectRepository Projects { get; }
    IPeriodRepository Periods { get; }
    IPersonRepository People { get; }
    IRoleRepository Roles { get; }
    IPersonRoleRepository PersonRoles { get; }
    IDevGroupRepository DevGroups { get; }
    IDevGroupMemberRepository DevGroupMembers { get; }
    ITaskGroupRepository TaskGroups { get; }
    IPlanningTaskRepository PlanningTasks { get; }
    IImportBatchRepository ImportBatches { get; }
    IAppSettingRepository AppSettings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre una transacción para operaciones que deben ser todo-o-nada, como la
    /// importación. Se declara aquí y no con el tipo de EF para no acoplar el dominio
    /// a un proveedor concreto.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
