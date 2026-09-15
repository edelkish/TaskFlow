using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class SettingsService : ISettingsService
{
    private const string ColorModeKey = "theme.colorMode";
    private const string SidebarBgKey = "theme.sidebarBg";
    private const string HeaderBgKey = "theme.headerBg";
    private const string FooterBgKey = "theme.footerBg";
    private const string PrimaryKey = "theme.primary";

    private readonly IUnitOfWork _unitOfWork;

    public SettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ThemeSettingsDto>> GetThemeAsync(CancellationToken ct = default)
    {
        var settings = await _unitOfWork.AppSettings.GetAllAsync();

        return Result<ThemeSettingsDto>.Success(new ThemeSettingsDto
        {
            ColorMode = GetOr(settings, ColorModeKey, "light"),
            SidebarBg = GetOr(settings, SidebarBgKey, "bg-body-secondary"),
            HeaderBg = GetOr(settings, HeaderBgKey, "bg-body"),
            FooterBg = GetOr(settings, FooterBgKey, "bg-body"),
            Primary = GetOr(settings, PrimaryKey, "#467FD0")
        });
    }

    public async Task<Result<ThemeSettingsDto>> UpdateThemeAsync(UpdateThemeSettingsDto dto, CancellationToken ct = default)
    {
        await SetAsync(ColorModeKey, dto.ColorMode, "Modo de color del sistema (light/dark)", ct);
        await SetAsync(SidebarBgKey, dto.SidebarBg, "Clase de fondo de la barra lateral", ct);
        await SetAsync(HeaderBgKey, dto.HeaderBg, "Clase de fondo del encabezado", ct);
        await SetAsync(FooterBgKey, dto.FooterBg, "Clase de fondo del pie de página", ct);
        await SetAsync(PrimaryKey, dto.Primary, "Color primario del tema", ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return await GetThemeAsync(ct);
    }

    private async Task SetAsync(string key, string value, string description, CancellationToken ct)
    {
        var setting = await _unitOfWork.AppSettings.GetByKeyAsync(key);
        if (setting == null)
        {
            await _unitOfWork.AppSettings.AddAsync(new AppSetting
            {
                Key = key,
                Value = value,
                Description = description,
                IsActive = true
            });
        }
        else
        {
            setting.Value = value;
            await _unitOfWork.AppSettings.UpdateAsync(setting);
        }
    }

    private static string GetOr(IEnumerable<AppSetting> settings, string key, string fallback)
    {
        return settings.FirstOrDefault(s => s.Key == key)?.Value ?? fallback;
    }
}