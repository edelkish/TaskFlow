using AutoMapper;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public TaskService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<TaskDto>> GetByIdAsync(Guid id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
            return Result<TaskDto>.Failure($"Task with id {id} not found");

        var dto = _mapper.Map<TaskDto>(task);
        return Result<TaskDto>.Success(dto);
    }

    public async Task<Result<IEnumerable<TaskDto>>> GetByProjectAsync(Guid projectId)
    {
        var tasks = await _unitOfWork.Tasks.GetTasksByProjectAsync(projectId);
        var dtos = _mapper.Map<IEnumerable<TaskDto>>(tasks);
        return Result<IEnumerable<TaskDto>>.Success(dtos);
    }

    public async Task<Result<IEnumerable<TaskDto>>> GetByStatusAsync(TaskItemStatus status)
    {
        var tasks = await _unitOfWork.Tasks.GetTasksByStatusAsync(status);
        var dtos = _mapper.Map<IEnumerable<TaskDto>>(tasks);
        return Result<IEnumerable<TaskDto>>.Success(dtos);
    }

    public async Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto)
    {
        var task = _mapper.Map<TaskItem>(dto);

        var created = await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<TaskDto>(created);
        return Result<TaskDto>.Success(resultDto);
    }

    public async Task<Result<TaskDto>> UpdateAsync(Guid id, UpdateTaskDto dto)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
            return Result<TaskDto>.Failure($"Task with id {id} not found");

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.Priority = dto.Priority;
        task.DueDate = dto.DueDate;
        task.AssignedToId = dto.AssignedToId;
        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<TaskDto>(task);
        return Result<TaskDto>.Success(resultDto);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
            return Result<bool>.Failure($"Task with id {id} not found");

        await _unitOfWork.Tasks.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
