using AutoMapper;
using FluentValidation;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class PlanningTaskService : IPlanningTaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreatePlanningTaskDto> _createValidator;
    private readonly IValidator<UpdatePlanningTaskDto> _updateValidator;

    public PlanningTaskService(IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<CreatePlanningTaskDto> createValidator,
        IValidator<UpdatePlanningTaskDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PlanningTaskDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _unitOfWork.PlanningTasks.GetByIdAsync(id);
        if (task == null)
            return Result<PlanningTaskDto>.Failure("Tarea de planificación no encontrada.");

        return Result<PlanningTaskDto>.Success(_mapper.Map<PlanningTaskDto>(task));
    }

    public async Task<Result<IEnumerable<PlanningTaskDto>>> GetByGroupAsync(Guid taskGroupId, CancellationToken ct = default)
    {
        var tasks = await _unitOfWork.PlanningTasks.GetByGroupOrderedAsync(taskGroupId);
        return Result<IEnumerable<PlanningTaskDto>>.Success(
            _mapper.Map<IEnumerable<PlanningTaskDto>>(tasks));
    }

    public async Task<Result<IEnumerable<PlanningTaskDto>>> GetBacklogAsync(Guid? projectId, Guid? assigneeId, CancellationToken ct = default)
    {
        var tasks = await _unitOfWork.PlanningTasks.GetBacklogAsync(projectId, assigneeId);
        return Result<IEnumerable<PlanningTaskDto>>.Success(
            _mapper.Map<IEnumerable<PlanningTaskDto>>(tasks));
    }

    public async Task<Result<PlanningTaskDto>> CreateAsync(CreatePlanningTaskDto dto, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<PlanningTaskDto>.Failure(validation.ToString());

        // Tarea de periodo: el grupo define el proyecto. Tarea de backlog: el proyecto
        // viene explícito. Un grupo QA-only no tiene proyecto, y su tarea tampoco: es un
        // estado válido que el formato TXT genera.
        TaskGroup? group = null;
        Guid? projectId;

        if (dto.TaskGroupId.HasValue)
        {
            group = await _unitOfWork.TaskGroups.GetByIdAsync(dto.TaskGroupId.Value);
            if (group == null)
                return Result<PlanningTaskDto>.Failure("Grupo de tareas no encontrado.");

            projectId = group.ProjectId;

            if (!IsPersonInGroup(group, dto.AssignedPersonId))
                return Result<PlanningTaskDto>.Failure("La persona asignada no pertenece al grupo.");
        }
        else
        {
            if (!dto.ProjectId.HasValue)
            {
                return Result<PlanningTaskDto>.Failure(
                    "Indique un grupo (tarea de periodo) o un proyecto (tarea de backlog).");
            }

            projectId = dto.ProjectId.Value;
        }

        if (projectId.HasValue)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId.Value);
            if (project == null)
                return Result<PlanningTaskDto>.Failure("Proyecto no encontrado.");
        }

        // La numeración solo aplica a tareas de periodo. En backlog se ignora en lugar de
        // fallar: el DTO es el mismo para las dos variantes.
        var isBacklog = !dto.TaskGroupId.HasValue;

        var task = new PlanningTask
        {
            TaskGroupId = dto.TaskGroupId,
            ProjectId = projectId,
            Number = isBacklog ? null : dto.Number,
            SubNumber = isBacklog ? null : dto.SubNumber,
            AssignedPersonId = dto.AssignedPersonId,
            Description = dto.Description,
            Source = TaskSource.Manual,
            IsActive = true
        };

        await _unitOfWork.PlanningTasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetAsync(task.Id, ct);
    }

    public async Task<Result<PlanningTaskDto>> UpdateAsync(Guid id, UpdatePlanningTaskDto dto, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<PlanningTaskDto>.Failure(validation.ToString());

        var task = await _unitOfWork.PlanningTasks.GetByIdAsync(id);
        if (task == null)
            return Result<PlanningTaskDto>.Failure("Tarea de planificación no encontrada.");

        if (task.TaskGroupId.HasValue)
        {
            var group = await _unitOfWork.TaskGroups.GetByIdAsync(task.TaskGroupId.Value);
            if (group == null)
                return Result<PlanningTaskDto>.Failure("Grupo de tareas no encontrado.");

            if (!IsPersonInGroup(group, dto.AssignedPersonId))
                return Result<PlanningTaskDto>.Failure("La persona asignada no pertenece al grupo.");
        }

        var isBacklog = !task.TaskGroupId.HasValue;

        task.Number = isBacklog ? null : dto.Number;
        task.SubNumber = isBacklog ? null : dto.SubNumber;
        task.AssignedPersonId = dto.AssignedPersonId;
        task.Description = dto.Description;
        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.PlanningTasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<Result<PlanningTaskDto>> ReassignAsync(Guid id, Guid? assignedPersonId, CancellationToken ct = default)
    {
        var task = await _unitOfWork.PlanningTasks.GetByIdAsync(id);
        if (task == null)
            return Result<PlanningTaskDto>.Failure("Tarea de planificación no encontrada.");

        // En backlog la asignación es libre: no hay grupo que la restrinja.
        if (task.TaskGroupId.HasValue)
        {
            var group = await _unitOfWork.TaskGroups.GetByIdAsync(task.TaskGroupId.Value);
            if (group == null)
                return Result<PlanningTaskDto>.Failure("Grupo de tareas no encontrado.");

            if (!IsPersonInGroup(group, assignedPersonId))
                return Result<PlanningTaskDto>.Failure("La persona asignada no pertenece al grupo.");
        }

        task.AssignedPersonId = assignedPersonId;
        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.PlanningTasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _unitOfWork.PlanningTasks.GetByIdAsync(id);
        if (task == null)
            return Result<bool>.Failure("Tarea de planificación no encontrada.");

        await _unitOfWork.PlanningTasks.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private static bool IsPersonInGroup(TaskGroup group, Guid? personId)
    {
        if (personId == null) return true;
        return group.DevPersonId == personId
               || group.TeamLeadPersonId == personId
               || group.QaPersonId == personId;
    }
}
