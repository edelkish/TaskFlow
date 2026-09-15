using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces;

public interface ITaskFileParser
{
    Task<ParsedTaskFileDto> ParseAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IImportService
{
    Task<ImportResultDto> ImportTaskFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IEnumerable<ImportBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default);
    Task<ImportBatchDto?> GetBatchAsync(Guid importBatchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TaskGroupDto>> GetGroupsByPeriodAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<TaskGroupDto?> GetGroupAsync(Guid taskGroupId, CancellationToken cancellationToken = default);
}