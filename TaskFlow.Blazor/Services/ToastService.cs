using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TaskFlow.Blazor.Services;

public class ToastService
{
    private readonly IJSRuntime _js;

    public ToastService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SuccessAsync(string message)
        => await _js.InvokeVoidAsync("taskflow.notify.success", message);

    public async Task ErrorAsync(string message)
        => await _js.InvokeVoidAsync("taskflow.notify.error", message);

    public async Task InfoAsync(string message)
        => await _js.InvokeVoidAsync("taskflow.notify.info", message);

    public async Task WarningAsync(string message)
        => await _js.InvokeVoidAsync("taskflow.notify.warning", message);

    public async Task<bool> ValidateFormAsync(ElementReference form)
    {
        try
        {
            return await _js.InvokeAsync<bool>("taskflow.validation.validateForm", form);
        }
        catch
        {
            return true;
        }
    }
}