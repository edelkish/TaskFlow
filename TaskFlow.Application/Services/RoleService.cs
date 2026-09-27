using AutoMapper;
using FluentValidation;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateRoleDto> _createValidator;
    private readonly IValidator<UpdateRoleDto> _updateValidator;

    public RoleService(IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<CreateRoleDto> createValidator,
        IValidator<UpdateRoleDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<IEnumerable<RoleDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var roles = await _unitOfWork.Roles.GetAllAsync();
        var counts = await _unitOfWork.PersonRoles.GetPersonCountsByRoleAsync();

        var dtos = roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                IsActive = r.IsActive,
                PersonCount = counts.TryGetValue(r.Id, out var c) ? c : 0
            })
            .ToList();

        return Result<IEnumerable<RoleDto>>.Success(dtos);
    }

    public async Task<Result<RoleDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id);
        if (role == null)
            return Result<RoleDto>.Failure("Cargo no encontrado.");

        var dto = _mapper.Map<RoleDto>(role);
        return Result<RoleDto>.Success(dto with { PersonCount = 0 });
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleDto dto, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<RoleDto>.Failure(validation.ToString());

        var trimmed = dto.Name.Trim();

        var existing = await _unitOfWork.Roles.GetByNameCaseInsensitiveAsync(trimmed);
        if (existing != null)
            return Result<RoleDto>.Failure($"Ya existe un cargo con el nombre '{trimmed}'.");

        var role = new Role { Name = trimmed, IsActive = true };

        await _unitOfWork.Roles.AddAsync(role);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<RoleDto>.Success(_mapper.Map<RoleDto>(role));
    }

    public async Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleDto dto, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<RoleDto>.Failure(validation.ToString());

        var role = await _unitOfWork.Roles.GetByIdAsync(id);
        if (role == null)
            return Result<RoleDto>.Failure("Cargo no encontrado.");

        var trimmed = dto.Name.Trim();

        // Sin esta comprobación, renombrar a un nombre ya usado revienta el índice único
        // como un 500 en vez de un 400 con mensaje.
        var existing = await _unitOfWork.Roles.GetByNameCaseInsensitiveAsync(trimmed);
        if (existing != null && existing.Id != id)
            return Result<RoleDto>.Failure($"Ya existe un cargo con el nombre '{trimmed}'.");

        role.Name = trimmed;
        role.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Roles.UpdateAsync(role);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<RoleDto>.Success(_mapper.Map<RoleDto>(role));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id);
        if (role == null)
            return Result<bool>.Failure("Cargo no encontrado.");

        var counts = await _unitOfWork.PersonRoles.GetPersonCountsByRoleAsync();
        if (counts.TryGetValue(id, out var count) && count > 0)
        {
            return Result<bool>.Failure(
                $"No se puede eliminar '{role.Name}': {count} persona(s) lo tienen asignado. " +
                "Quítaselo primero para no dejar a esas personas sin cargo y romper la importación.");
        }

        await _unitOfWork.Roles.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
