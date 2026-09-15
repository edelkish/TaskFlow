using Microsoft.JSInterop;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Blazor.Services;

/// <summary>
/// Carga las configuraciones del sistema (theme) y las aplica sobre el DOM al iniciar.
/// </summary>
public class ThemeState
{
    private readonly ApiClient _apiClient;
    private readonly IJSRuntime _js;
    private bool _initialized;

    public ThemeSettingsDto? Current { get; private set; }

    public ThemeState(ApiClient apiClient, IJSRuntime js)
    {
        _apiClient = apiClient;
        _js = js;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            var dto = await _apiClient.GetThemeSettingsAsync();
            if (dto != null)
            {
                Current = dto;
            }
        }
        catch
        {
            // Si el API no responde aún se mantiene el tema por defecto del CSS.
        }

        await ApplyAsync();
        _initialized = true;
    }

    public async Task ApplyAsync()
    {
        if (Current == null)
        {
            return;
        }

        try
        {
            await _js.InvokeVoidAsync("applyTaskFlowTheme", Current);
        }
        catch
        {
        }
    }

    public async Task SaveAsync(UpdateThemeSettingsDto dto)
    {
        var updated = await _apiClient.UpdateThemeSettingsAsync(dto);
        if (updated != null)
        {
            Current = updated;
            await ApplyAsync();
        }
    }
}