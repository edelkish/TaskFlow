using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Interfaces;

public interface ITaskRepository : IGenericRepository<TaskItem>
{
    Task<IEnumerable<TaskItem>> GetTasksByProjectAsync(Guid projectId);
    Task<IEnumerable<TaskItem>> GetTasksByStatusAsync(TaskItemStatus status);
    Task<IEnumerable<TaskItem>> GetTasksByAssigneeAsync(Guid assigneeId);
}
