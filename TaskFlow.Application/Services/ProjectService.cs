using AutoMapper;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
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

        var dto = _mapper.Map<ProjectDto>(project);
        return Result<ProjectDto>.Success(dto);
    }

    public async Task<Result<IEnumerable<ProjectDto>>> GetAllAsync()
    {
        var projects = await _unitOfWork.Projects.GetAllAsync();
        var dtos = _mapper.Map<IEnumerable<ProjectDto>>(projects);
        return Result<IEnumerable<ProjectDto>>.Success(dtos);
    }

    public async Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto, Guid ownerId)
    {
        if (dto.EndDate.HasValue && dto.EndDate.Value.Date < dto.StartDate.Date)
            return Result<ProjectDto>.Failure("End date must be on or after the start date");

        var project = _mapper.Map<Project>(dto);
        project.OwnerId = ownerId;
        project.Progress = Math.Clamp(dto.Progress, 0, 100);

        var created = await _unitOfWork.Projects.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<ProjectDto>(created);
        return Result<ProjectDto>.Success(resultDto);
    }

    public async Task<Result<ProjectDto>> UpdateAsync(Guid id, UpdateProjectDto dto)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
            return Result<ProjectDto>.Failure($"Project with id {id} not found");

        if (dto.EndDate.HasValue && dto.EndDate.Value.Date < project.StartDate.Date)
            return Result<ProjectDto>.Failure("End date must be on or after the start date");

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.EndDate = dto.EndDate;
        project.Version = dto.Version;
        project.Progress = Math.Clamp(dto.Progress, 0, 100);
        project.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Projects.UpdateAsync(project);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<ProjectDto>(project);
        return Result<ProjectDto>.Success(resultDto);
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
}
