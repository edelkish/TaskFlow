using AutoMapper;
using FluentValidation;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class TaskGroupService : ITaskGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateTaskGroupDto> _createValidator;
    private readonly IValidator<UpdateTaskGroupDto> _updateValidator;

    public TaskGroupService(IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<CreateTaskGroupDto> createValidator,
        IValidator<UpdateTaskGroupDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<TaskGroupDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var group = await _unitOfWork.TaskGroups.GetWithTasksAsync(id);
        if (group == null)
            return Result<TaskGroupDto>.Failure("Grupo de tareas no encontrado.");

        return Result<TaskGroupDto>.Success(_mapper.Map<TaskGroupDto>(group));
    }

    public async Task<Result<IEnumerable<TaskGroupDto>>> GetByPeriodAsync(Guid periodId, CancellationToken ct = default)
    {
        var groups = await _unitOfWork.TaskGroups.GetByPeriodAsync(periodId);
        return Result<IEnumerable<TaskGroupDto>>.Success(_mapper.Map<IEnumerable<TaskGroupDto>>(groups));
    }

    public async Task<Result<IEnumerable<TaskGroupDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var groups = await _unitOfWork.TaskGroups.GetAllAsync();
        return Result<IEnumerable<TaskGroupDto>>.Success(_mapper.Map<IEnumerable<TaskGroupDto>>(groups));
    }

    public async Task<Result<TaskGroupDto>> CreateAsync(CreateTaskGroupDto dto, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<TaskGroupDto>.Failure(validation.ToString());

        var group = new TaskGroup
        {
            PeriodId = dto.PeriodId,
            ProjectId = dto.ProjectId,
            DevPersonId = dto.DevPersonId,
            TeamLeadPersonId = dto.TeamLeadPersonId,
            QaPersonId = dto.QaPersonId,
            IsActive = true
        };

        await _unitOfWork.TaskGroups.AddAsync(group);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetAsync(group.Id, ct);
    }

    public async Task<Result<TaskGroupDto>> UpdateAsync(Guid id, UpdateTaskGroupDto dto, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<TaskGroupDto>.Failure(validation.ToString());

        var group = await _unitOfWork.TaskGroups.GetByIdAsync(id);
        if (group == null)
            return Result<TaskGroupDto>.Failure("Grupo de tareas no encontrado.");

        if (dto.PeriodId.HasValue) group.PeriodId = dto.PeriodId.Value;
        group.ProjectId = dto.ProjectId;
        group.DevPersonId = dto.DevPersonId;
        group.TeamLeadPersonId = dto.TeamLeadPersonId;
        group.QaPersonId = dto.QaPersonId;

        await _unitOfWork.TaskGroups.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetAsync(group.Id, ct);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var group = await _unitOfWork.TaskGroups.GetByIdAsync(id);
        if (group == null)
            return Result<bool>.Failure("Grupo de tareas no encontrado.");

        await _unitOfWork.TaskGroups.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}