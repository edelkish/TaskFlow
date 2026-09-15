using System.Net.Http.Headers;

namespace TaskFlow.Blazor.Services;

public class AuthTokenHandler : DelegatingHandler
{
    private readonly AuthTokenStore _tokenStore;

    public AuthTokenHandler(AuthTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _tokenStore.Token;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}