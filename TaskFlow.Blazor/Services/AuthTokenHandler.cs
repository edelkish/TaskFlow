using System.Net.Http.Headers;

namespace TaskFlow.Blazor.Services;

public class AuthTokenHandler : DelegatingHandler
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(5);

    private readonly AuthTokenStore _tokenStore;

    public AuthTokenHandler(AuthTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await WaitUntilReadyAsync(cancellationToken);

        var token = _tokenStore.Token;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task WaitUntilReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _tokenStore.Ready.WaitAsync(ReadyTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            // AuthState.LoadAsync() no llegó a marcar el gate a tiempo; se sigue sin
            // token en vez de colgar el pedido para siempre.
        }
    }
}
