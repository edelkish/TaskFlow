using AutoMapper;
using FluentValidation;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class PeriodService : IPeriodService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreatePeriodDto> _createValidator;
    private readonly IValidator<UpdatePeriodDto> _updateValidator;

    public PeriodService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreatePeriodDto> createValidator,
        IValidator<UpdatePeriodDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<IEnumerable<PeriodDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var periods = await _unitOfWork.Periods.GetOrderedDescWithRelatedAsync();
        return Result<IEnumerable<PeriodDto>>.Success(_mapper.Map<IEnumerable<PeriodDto>>(periods));
    }

    public async Task<Result<PeriodDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var period = await _unitOfWork.Periods.GetWithRelatedAsync(id);
        if (period == null)
            return Result<PeriodDto>.Failure("Periodo no encontrado.");

        return Result<PeriodDto>.Success(_mapper.Map<PeriodDto>(period));
    }

    public async Task<Result<PeriodDto>> CreateAsync(CreatePeriodDto dto, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<PeriodDto>.Failure(validation.ToString());

        var existing = await _unitOfWork.Periods.GetByMonthYearAsync(dto.Month, dto.Year);
        if (existing != null)
            return Result<PeriodDto>.Failure($"Ya existe el periodo para {dto.Month}/{dto.Year}.");

        var period = new Period
        {
            Month = dto.Month,
            Year = dto.Year,
            Name = dto.Name,
            IsActive = true
        };

        await _unitOfWork.Periods.AddAsync(period);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PeriodDto>.Success(_mapper.Map<PeriodDto>(period));
    }

    public async Task<Result<PeriodDto>> UpdateAsync(Guid id, UpdatePeriodDto dto, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<PeriodDto>.Failure(validation.ToString());

        var period = await _unitOfWork.Periods.GetByIdAsync(id);
        if (period == null)
            return Result<PeriodDto>.Failure("Periodo no encontrado.");

        // Anio y mes no se tocan. Son la identidad del periodo y los grupos y tareas cuelgan
        // de ella, asi que moverlos reubicaria contenido de un mes a otro. El nombre es la
        // unica etiqueta libre que el usuario puede corregir.
        period.Name = dto.Name.Trim();
        period.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Periods.UpdateAsync(period);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var period = await _unitOfWork.Periods.GetByIdAsync(id);
        if (period == null)
            return Result<bool>.Failure("Periodo no encontrado.");

        var groups = await _unitOfWork.Periods.CountTaskGroupsAsync(id);
        var imports = await _unitOfWork.Periods.CountImportBatchesAsync(id);

        // Se bloquea en vez de arrastrar en cascada. Las tareas del mes son historico de
        // planificacion, y decidir si se borran o se reubican es del usuario, no un efecto
        // secundario de eliminar una fila. La BD lo impide igual por FK Restrict/NoAction, y
        // TaskFlowDbContext convierte esa violacion en un 400 legible si se cuela entre el
        // conteo y el borrado: esto solo da el mensaje concreto con las cantidades.
        if (groups > 0 || imports > 0)
        {
            return Result<bool>.Failure(BlockingReason(period.Name, groups, imports));
        }

        await _unitOfWork.Periods.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    private static string BlockingReason(string name, int groups, int imports) =>
        $"No se puede eliminar '{name}': tiene {Describe("grupo de tareas", "grupos de tareas", groups)} " +
        $"y {Describe("importación", "importaciones", imports)}. " +
        "Elimine primero el contenido asociado.";

    /// <summary>Singular o plural, para que el mensaje se lea bien sea cual sea el numero.</summary>
    private static string Describe(string singular, string plural, int count) =>
        count == 1 ? $"1 {singular}" : $"{count} {plural}";
}