using AutoMapper;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProjectService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProjectDto>> GetByIdAsync(Guid id)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            return Result<ProjectDto>.Failure($"Project with id {id} not found");

        return Result<ProjectDto>.Success(await MapAsync(project));
    }

    public async Task<Result<IEnumerable<ProjectDto>>> GetAllAsync()
    {
        var projects = await _unitOfWork.Projects.GetAllAsync();
        var dtos = new List<ProjectDto>();

        foreach (var project in projects)
        {
            dtos.Add(await MapAsync(project));
        }

        return Result<IEnumerable<ProjectDto>>.Success(
            dtos.OrderBy(d => d.Name).ToList());
    }

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, Guid ownerId)
    {
        if (dto.EndDate.HasValue && dto.EndDate.Value.Date < dto.StartDate.Date)
            return Result<ProjectDto>.Failure("End date must be on or after the start date");

        if (dto.DevGroupId.HasValue)
        {
            var group = await _unitOfWork.DevGroups.GetByIdAsync(dto.DevGroupId.Value);
            if (group == null)
                return Result<ProjectDto>.Failure("El grupo de desarrollo asignado no existe.");
        }

        var project = _mapper.Map<Project>(dto);
        project.OwnerId = ownerId;
        project.Progress = Math.Clamp(dto.Progress, 0, 100);

        var created = await _unitOfWork.Projects.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return Result<ProjectDto>.Success(await MapAsync(created));
    }

    public async Task<Result<ProjectDto>> UpdateAsync(Guid id, UpdateProjectDto dto)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            return Result<ProjectDto>.Failure($"Project with id {id} not found");

        if (dto.EndDate.HasValue && dto.EndDate.Value.Date < project.StartDate.Date)
            return Result<ProjectDto>.Failure("End date must be on or after the start date");

        if (dto.DevGroupId.HasValue)
        {
            var group = await _unitOfWork.DevGroups.GetByIdAsync(dto.DevGroupId.Value);
            if (group == null)
                return Result<ProjectDto>.Failure("El grupo de desarrollo asignado no existe.");
        }

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.EndDate = dto.EndDate;
        project.Version = dto.Version;
        project.Progress = Math.Clamp(dto.Progress, 0, 100);
        project.DevGroupId = dto.DevGroupId;
        project.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Projects.UpdateAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return Result<ProjectDto>.Success(await MapAsync(project));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            return Result<bool>.Failure($"Project with id {id} not found");

        await _unitOfWork.Projects.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// TaskCount cuenta el backlog (tareas sin grupo), no el histórico mensual. Antes
    /// venía de Project.Tasks, la tabla TaskItem huérfana que siempre daba 0.
    /// </summary>
    private async Task<ProjectDto> MapAsync(Project project)
    {
        var dto = _mapper.Map<ProjectDto>(project);

        string? groupName = null;
        if (project.DevGroupId.HasValue)
        {
            var group = await _unitOfWork.DevGroups.GetByIdAsync(project.DevGroupId.Value);
            groupName = group?.Name;
        }

        var backlog = (await _unitOfWork.PlanningTasks.GetAllAsync())
            .Count(t => t.ProjectId == project.Id && t.TaskGroupId == null);

        return dto with { DevGroupName = groupName, TaskCount = backlog };
    }
}
