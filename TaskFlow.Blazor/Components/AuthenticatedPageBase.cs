using Microsoft.AspNetCore.Components;
using TaskFlow.Blazor.Services;

namespace TaskFlow.Blazor.Components;

public abstract class AuthenticatedPageBase : ComponentBase
{
    [Inject] protected AuthState AuthState { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected ToastService Toast { get; set; } = default!;

    protected string? LoadError { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await AuthState.LoadAsync();
        if (!AuthState.IsAuthenticated)
        {
            Nav.NavigateTo("/login");
            return;
        }

        if (!string.IsNullOrEmpty(LoadError))
        {
            await Toast.ErrorAsync(LoadError!);
            LoadError = null;
        }
    }
}