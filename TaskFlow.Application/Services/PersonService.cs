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
        return Result<IEnumerable<PersonDto>>.Success(_mapper.Map<IEnumerable<PersonDto>>(people));
    }

    public async Task<Result<PersonDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var person = await _unitOfWork.People.GetByIdAsync(id);
        if (person == null)
            return Result<PersonDto>.Failure("Persona no encontrada.");

        return Result<PersonDto>.Success(_mapper.Map<PersonDto>(person));
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

        await _unitOfWork.People.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}