using FluentValidation;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class DevGroupService : IDevGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateDevGroupDto> _createValidator;
    private readonly IValidator<UpdateDevGroupDto> _updateValidator;

    public DevGroupService(IUnitOfWork unitOfWork,
        IValidator<CreateDevGroupDto> createValidator,
        IValidator<UpdateDevGroupDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<IEnumerable<DevGroupDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var groups = await _unitOfWork.DevGroups.GetAllAsync();
        var dtos = new List<DevGroupDto>();

        foreach (var group in groups)
        {
            var memberIds = await _unitOfWork.DevGroupMembers.GetMemberIdsAsync(group.Id);
            dtos.Add(await MapAsync(group, memberIds, cancellationToken));
        }

        return Result<IEnumerable<DevGroupDto>>.Success(
            dtos.OrderBy(d => d.Name).ToList());
    }

    public async Task<Result<DevGroupDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.DevGroups.GetByIdAsync(id);
        if (group == null)
            return Result<DevGroupDto>.Failure("Grupo de desarrollo no encontrado.");

        var withMembers = await _unitOfWork.DevGroups.GetWithMembersAsync(id);
        var dto = new DevGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsActive = group.IsActive,
            MemberCount = withMembers?.Members.Count ?? 0,
            Members = withMembers?.Members
                .Select(m => new DevGroupMemberDto { PersonId = m.PersonId, PersonName = PersonDisplayName.For(m.Person.Name, m.Person.LastName) })
                .OrderBy(m => m.PersonName)
                .ToList() ?? new List<DevGroupMemberDto>()
        };

        return Result<DevGroupDto>.Success(dto);
    }

    public async Task<Result<DevGroupDto>> CreateAsync(CreateDevGroupDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return Result<DevGroupDto>.Failure(validation.ToString());

        var trimmed = dto.Name.Trim();

        var existing = await _unitOfWork.DevGroups.GetByNameCaseInsensitiveAsync(trimmed);
        if (existing != null)
            return Result<DevGroupDto>.Failure($"Ya existe un grupo de desarrollo con el nombre '{trimmed}'.");

        var missing = await ResolveUnknownPeopleAsync(dto.MemberIds, cancellationToken);
        if (missing.Count > 0)
        {
            return Result<DevGroupDto>.Failure(
                $"No se encontraron estas personas: {string.Join(", ", missing)}.");
        }

        var group = new DevGroup
        {
            Name = trimmed,
            Description = dto.Description?.Trim(),
            IsActive = true
        };

        await _unitOfWork.DevGroups.AddAsync(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _unitOfWork.DevGroupMembers.ReplaceMembersAsync(group.Id, dto.MemberIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(group.Id, cancellationToken);
    }

    public async Task<Result<DevGroupDto>> UpdateAsync(Guid id, UpdateDevGroupDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return Result<DevGroupDto>.Failure(validation.ToString());

        var group = await _unitOfWork.DevGroups.GetByIdAsync(id);
        if (group == null)
            return Result<DevGroupDto>.Failure("Grupo de desarrollo no encontrado.");

        var trimmed = dto.Name.Trim();

        var existing = await _unitOfWork.DevGroups.GetByNameCaseInsensitiveAsync(trimmed);
        if (existing != null && existing.Id != id)
            return Result<DevGroupDto>.Failure($"Ya existe un grupo de desarrollo con el nombre '{trimmed}'.");

        group.Name = trimmed;
        group.Description = dto.Description?.Trim();
        group.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.DevGroups.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.DevGroups.GetByIdAsync(id);
        if (group == null)
            return Result<bool>.Failure("Grupo de desarrollo no encontrado.");

        await _unitOfWork.DevGroupMembers.RemoveByGroupAsync(id);

        // Los proyectos quedan con DevGroupId NULL gracias al ON DELETE SET NULL: borrar un
        // grupo no debe dejar proyectos apuntando a un grupo inexistente.
        await _unitOfWork.DevGroups.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<DevGroupDto>> SetMembersAsync(Guid id, List<Guid> personIds, CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.DevGroups.GetByIdAsync(id);
        if (group == null)
            return Result<DevGroupDto>.Failure("Grupo de desarrollo no encontrado.");

        var missing = await ResolveUnknownPeopleAsync(personIds, cancellationToken);
        if (missing.Count > 0)
        {
            return Result<DevGroupDto>.Failure(
                $"No se encontraron estas personas: {string.Join(", ", missing)}.");
        }

        await _unitOfWork.DevGroupMembers.ReplaceMembersAsync(id, personIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    private async Task<List<string>> ResolveUnknownPeopleAsync(IEnumerable<Guid> personIds, CancellationToken cancellationToken)
    {
        var requested = personIds.Distinct().ToList();
        if (requested.Count == 0)
            return new List<string>();

        var all = await _unitOfWork.People.GetAllAsync();
        var known = all.Select(p => p.Id).ToHashSet();

        return requested.Where(p => !known.Contains(p)).Select(p => p.ToString()).ToList();
    }

    private async Task<DevGroupDto> MapAsync(DevGroup group, IReadOnlyList<Guid> memberIds, CancellationToken cancellationToken)
    {
        var people = memberIds.Count == 0
            ? new List<Person>()
            : (await _unitOfWork.People.GetAllAsync()).Where(p => memberIds.Contains(p.Id)).ToList();

        var projects = (await _unitOfWork.Projects.GetAllAsync()).Count(p => p.DevGroupId == group.Id);

        return new DevGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsActive = group.IsActive,
            MemberCount = memberIds.Count,
            ProjectCount = projects,
            Members = people
                .Select(p => new DevGroupMemberDto { PersonId = p.Id, PersonName = PersonDisplayName.For(p.Name, p.LastName) })
                .OrderBy(m => m.PersonName)
                .ToList()
        };
    }
}
