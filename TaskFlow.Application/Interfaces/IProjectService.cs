using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces;

public interface IProjectService
{
    Task<Result<ProjectDto>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<ProjectDto>>> GetAllAsync();
    Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, Guid ownerId);
    Task<Result<ProjectDto>> UpdateAsync(Guid id, UpdateProjectDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
}
