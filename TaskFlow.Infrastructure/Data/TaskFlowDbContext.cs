using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Infrastructure.Data;

public class TaskFlowDbContext : IdentityDbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<PersonRole> PersonRoles => Set<PersonRole>();
    public DbSet<DevGroup> DevGroups => Set<DevGroup>();
    public DbSet<DevGroupMember> DevGroupMembers => Set<DevGroupMember>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<TaskGroup> TaskGroups => Set<TaskGroup>();
    public DbSet<PlanningTask> PlanningTasks => Set<PlanningTask>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(TaskFlowDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            // Se traduce aqui, en SaveChangesAsync, y no en cada servicio, porque es el unico
            // punto por el que pasan todas las escrituras. La capa Application no conoce EF,
            // asi que es el lugar natural para convertir el fallo de la base en algo que el
            // middleware ya sabe responder con 400 y un mensaje util.
            //
            // El mensaje no puede decir que registro concreto estorba sin inventarse el
            // nombre de la FK, pero si dice lo accionable: hay datos dependientes.
            throw new DomainException(
                "No se puede completar la operacion porque el registro tiene datos asociados. " +
                "Elimine primero los registros dependientes e intentelo de nuevo.",
                ex);
        }
    }

    /// <summary>
    /// 547 es el numero de error de SQL Server para una violacion de clave foranea. Se
    /// distingue de otros fallos de DbUpdateException (unicidad, required) para no
    /// enmascararlos con un mensaje que no corresponde.
    /// </summary>
    private static bool IsForeignKeyViolation(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 };
}