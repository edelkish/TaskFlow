using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Interfaces;

public interface ITaskService
{
    Task<Result<TaskDto>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<TaskDto>>> GetByProjectAsync(Guid projectId);
    Task<Result<IEnumerable<TaskDto>>> GetByStatusAsync(TaskItemStatus status);
    Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto);
    Task<Result<TaskDto>> UpdateAsync(Guid id, UpdateTaskDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
}
