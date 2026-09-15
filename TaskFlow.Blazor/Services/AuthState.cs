using System.Text.Json;
using Microsoft.JSInterop;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Blazor.Services;

public class AuthState
{
    private const string TokenKey = "tf_token";
    private const string EmailKey = "tf_email";
    private const string RolesKey = "tf_roles";
    private readonly AuthTokenStore _tokenStore;
    private readonly ApiClient _apiClient;
    private readonly IJSRuntime _js;

    public List<string> Roles { get; private set; } = new();

    public bool IsAuthenticated => _tokenStore.IsAuthenticated;
    public bool IsAdmin => Roles.Contains("Admin");
    public string? Email => _tokenStore.Email;

    public AuthState(AuthTokenStore tokenStore, ApiClient apiClient, IJSRuntime js)
    {
        _tokenStore = tokenStore;
        _apiClient = apiClient;
        _js = js;
    }

    public async Task LoadAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            var email = await _js.InvokeAsync<string?>("localStorage.getItem", EmailKey);
            var roles = await _js.InvokeAsync<string?>("localStorage.getItem", RolesKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                _tokenStore.Token = token;
                _tokenStore.Email = email ?? string.Empty;
                Roles = !string.IsNullOrWhiteSpace(roles)
                    ? JsonSerializer.Deserialize<List<string>>(roles) ?? new List<string>()
                    : new List<string>();
            }
        }
        catch
        {
            // Entorno prerender sin JS disponible; se recuperará en el ciclo interactivo.
        }
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var response = await _apiClient.LoginAsync(new LoginDto(email, password));
        if (response == null)
        {
            return false;
        }

        Apply(response.Token, response.Email, response.Roles);
        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, response.Token);
        await _js.InvokeVoidAsync("localStorage.setItem", EmailKey, response.Email);
        await _js.InvokeVoidAsync("localStorage.setItem", RolesKey, JsonSerializer.Serialize(response.Roles));
        return true;
    }

    public async Task LogoutAsync()
    {
        _tokenStore.Token = null;
        _tokenStore.Email = null;
        Roles = new List<string>();
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", EmailKey);
            await _js.InvokeVoidAsync("localStorage.removeItem", RolesKey);
        }
        catch
        {
        }
    }

    private void Apply(string token, string email, List<string> roles)
    {
        _tokenStore.Token = token;
        _tokenStore.Email = email;
        Roles = roles ?? new List<string>();
    }
}