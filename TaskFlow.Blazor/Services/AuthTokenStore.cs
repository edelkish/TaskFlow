namespace TaskFlow.Blazor.Services;

public class AuthTokenStore
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string? Token { get; set; }
    public string? Email { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    /// <summary>
    /// Se completa una vez que AuthState intento restaurar (o confirmar) la sesion
    /// de este circuito. AuthTokenHandler espera este gate antes de mandar cualquier
    /// pedido, para no disparar un 401 mientras el token todavia se esta cargando
    /// desde localStorage tras una recarga completa (F5) o un login/registro.
    /// </summary>
    public Task Ready => _ready.Task;

    public void MarkReady() => _ready.TrySetResult();
}
