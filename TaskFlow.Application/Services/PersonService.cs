using AutoMapper;
using FluentValidation;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class PersonService : IPersonService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreatePersonDto> _createValidator;
    private readonly IValidator<UpdatePersonDto> _updateValidator;

    public PersonService(IUnitOfWork unitOfWork, IMapper mapper,
        IValidator<CreatePersonDto> createValidator,
        IValidator<UpdatePersonDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<IEnumerable<PersonDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var people = await _unitOfWork.People.GetAllAsync();
        var dtos = await MapWithRolesAsync(people, ct);
        return Result<IEnumerable<PersonDto>>.Success(dtos);
    }

    public async Task<Result<PersonDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var person = await _unitOfWork.People.GetByIdAsync(id);
        if (person == null)
            return Result<PersonDto>.Failure("Persona no encontrada.");

        var mapped = await MapWithRolesAsync([person], ct);
        return Result<PersonDto>.Success(mapped.First());
    }

    /// <summary>
    /// Los cargos no se mapean con AutoMapper porque viven en la tabla puente
    /// PersonRole y requieren una consulta aparte.
    /// </summary>
    private async Task<List<PersonDto>> MapWithRolesAsync(IEnumerable<Person> people, CancellationToken ct)
    {
        var result = new List<PersonDto>();

        foreach (var person in people)
        {
            var roles = await _unitOfWork.PersonRoles.GetRolesByPersonAsync(person.Id);
            var dto = _mapper.Map<PersonDto>(person);
            result.Add(dto with
            {
                Roles = roles.Select(r => r.Name).ToList(),
                RoleIds = roles.Select(r => r.Id).ToList()
            });
        }

        return result;
    }

    public async Task<Result<PersonDto>> SetRolesAsync(Guid id, SetPersonRolesDto dto, CancellationToken ct = default)
    {
        var person = await _unitOfWork.People.GetByIdAsync(id);
        if (person == null)
            return Result<PersonDto>.Failure("Persona no encontrada.");

        var requested = dto.RoleIds.Distinct().ToList();

        if (requested.Count > 0)
        {
            var known = await _unitOfWork.Roles.GetAllAsync();
            var knownIds = known.Select(r => r.Id).ToHashSet();

            var unknown = requested.Where(r => !knownIds.Contains(r)).ToList();
            if (unknown.Count > 0)
            {
                return Result<PersonDto>.Failure(
                    $"Cargo(s) inexistente(s): {string.Join(", ", unknown.Select(u => u.ToString()))}.");
            }
        }

        var current = await _unitOfWork.PersonRoles.GetRoleIdsByPersonAsync(id);

        foreach (var roleId in current.Except(requested))
        {
            await _unitOfWork.PersonRoles.RemoveAsync(id, roleId);
        }

        foreach (var roleId in requested.Except(current))
        {
            await _unitOfWork.PersonRoles.AddAsync(new PersonRole
            {
                PersonId = id,
                RoleId = roleId
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<Result<PersonDto>> CreateAsync(CreatePersonDto dto, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<PersonDto>.Failure(validation.ToString());

        var existing = await _unitOfWork.People.GetByNameCaseInsensitiveAsync(dto.Name.Trim());
        if (existing != null)
            return Result<PersonDto>.Failure($"Ya existe una persona con el nombre '{dto.Name.Trim()}'.");

        var person = new Person
        {
            Name = dto.Name.Trim(),
            UserId = dto.UserId,
            IsActive = true
        };

        await _unitOfWork.People.AddAsync(person);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PersonDto>.Success(_mapper.Map<PersonDto>(person));
    }

    public async Task<Result<PersonDto>> UpdateAsync(Guid id, UpdatePersonDto dto, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<PersonDto>.Failure(validation.ToString());

        var person = await _unitOfWork.People.GetByIdAsync(id);
        if (person == null)
            return Result<PersonDto>.Failure("Persona no encontrada.");

        person.Name = dto.Name.Trim();
        person.UserId = dto.UserId;
        person.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.People.UpdateAsync(person);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PersonDto>.Success(_mapper.Map<PersonDto>(person));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var person = await _unitOfWork.People.GetByIdAsync(id);
        if (person == null)
            return Result<bool>.Failure("Persona no encontrada.");

        // Las asignaciones de cargo van en cascada con la persona.
        await _unitOfWork.PersonRoles.RemoveByPersonAsync(id);
        await _unitOfWork.People.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}