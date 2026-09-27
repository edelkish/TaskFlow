using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces;

public interface ITaskFileParser
{
    Task<ParsedTaskFileDto> ParseAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IImportService
{
    /// <summary>Previsualización: analiza el archivo sin escribir nada.</summary>
    Task<ImportValidationDto> ValidateAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida y, solo si no hay errores, importa el archivo completo de forma atómica.
    /// Si hay errores devuelve <see cref="ImportExecutionDto"/> con Accepted = false.
    /// </summary>
    Task<ImportExecutionDto> ImportTaskFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IEnumerable<ImportBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default);
    Task<ImportBatchDto?> GetBatchAsync(Guid importBatchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskGroupDto>> GetGroupsByPeriodAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<TaskGroupDto?> GetGroupAsync(Guid taskGroupId, CancellationToken cancellationToken = default);
}
