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

    public PeriodService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreatePeriodDto> createValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
    }

    public async Task<Result<IEnumerable<PeriodDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var periods = await _unitOfWork.Periods.GetOrderedDescAsync();
        return Result<IEnumerable<PeriodDto>>.Success(_mapper.Map<IEnumerable<PeriodDto>>(periods));
    }

    public async Task<Result<PeriodDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var period = await _unitOfWork.Periods.GetByIdAsync(id);
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
}