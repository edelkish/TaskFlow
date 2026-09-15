namespace TaskFlow.Blazor.Services;

public class AuthTokenStore
{
    public string? Token { get; set; }
    public string? Email { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
}