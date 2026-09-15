using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces;

public interface ISettingsService
{
    Task<Result<ThemeSettingsDto>> GetThemeAsync(CancellationToken ct = default);
    Task<Result<ThemeSettingsDto>> UpdateThemeAsync(UpdateThemeSettingsDto dto, CancellationToken ct = default);
}