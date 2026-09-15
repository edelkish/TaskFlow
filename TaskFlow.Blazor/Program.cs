using TaskFlow.Blazor.Components;
using TaskFlow.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// API Client: el token JWT vive en AuthTokenStore (singleton) y el AuthTokenHandler
// lo adjunta a cada petición, con lo que todas las instancias de ApiClient comparten la sesión.
builder.Services.AddSingleton<AuthTokenStore>();
builder.Services.AddTransient<AuthTokenHandler>();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7185/");
})
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddScoped<AuthState>();

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
