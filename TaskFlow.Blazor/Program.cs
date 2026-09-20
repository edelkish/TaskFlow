using TaskFlow.Blazor.Components;
using TaskFlow.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// API Client: AuthTokenStore y el HttpClient de ApiClient son scoped (uno por
// circuito/usuario conectado). IHttpClientFactory no sirve aquí porque los handlers
// añadidos con AddHttpMessageHandler se resuelven en un scope propio del factory,
// desconectado del circuito de Blazor Server, así que el HttpClient se construye
// a mano dentro del scope correcto para que cada usuario use su propio token.
builder.Services.AddScoped<AuthTokenStore>();
builder.Services.AddScoped(sp =>
{
    var tokenStore = sp.GetRequiredService<AuthTokenStore>();
    var handler = new AuthTokenHandler(tokenStore) { InnerHandler = new HttpClientHandler() };
    return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5253/") };
});
builder.Services.AddScoped<ApiClient>();

builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<ToastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
