using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface IProjectRepository : IGenericRepository<Project>
{
    Task<IEnumerable<Project>> GetProjectsByOwnerAsync(Guid ownerId);
    Task<Project?> GetProjectWithTasksAsync(Guid projectId);
    Task<Project?> GetByNameAsync(string name);
    Task<Project?> GetByNameCaseInsensitiveAsync(string name);
}