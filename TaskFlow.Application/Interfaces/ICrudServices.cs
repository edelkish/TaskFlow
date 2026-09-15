using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces;

public interface ITaskGroupService
{
    Task<Result<TaskGroupDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<TaskGroupDto>>> GetByPeriodAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<TaskGroupDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<TaskGroupDto>> CreateAsync(CreateTaskGroupDto dto, CancellationToken cancellationToken = default);
    Task<Result<TaskGroupDto>> UpdateAsync(Guid id, UpdateTaskGroupDto dto, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPlanningTaskService
{
    Task<Result<IEnumerable<PlanningTaskDto>>> GetByGroupAsync(Guid taskGroupId, CancellationToken cancellationToken = default);
    Task<Result<PlanningTaskDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PlanningTaskDto>> CreateAsync(CreatePlanningTaskDto dto, CancellationToken cancellationToken = default);
    Task<Result<PlanningTaskDto>> UpdateAsync(Guid id, UpdatePlanningTaskDto dto, CancellationToken cancellationToken = default);
    Task<Result<PlanningTaskDto>> ReassignAsync(Guid id, Guid? assignedPersonId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPeriodService
{
    Task<Result<IEnumerable<PeriodDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<PeriodDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PeriodDto>> CreateAsync(CreatePeriodDto dto, CancellationToken cancellationToken = default);
}

public interface IPersonService
{
    Task<Result<IEnumerable<PersonDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<PersonDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PersonDto>> CreateAsync(CreatePersonDto dto, CancellationToken cancellationToken = default);
    Task<Result<PersonDto>> UpdateAsync(Guid id, UpdatePersonDto dto, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}