using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaskFlow.Blazor.Components;
using TaskFlow.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API Client: IHttpClientFactory construye los handlers agregados con
// AddHttpMessageHandler en un scope interno propio (creado desde el
// proveedor raíz), desconectado del scope donde se resuelve el resto de
// la app — esto pasa en CUALQUIER hosting model, no solo en Blazor Server
// (la causa de fondo es la misma que en docs/Fix-AuthTokenStore-Scope.md).
// En WASM la solución es distinta a la de Server: como cada pestaña del
// navegador es de por sí un solo usuario (no hay circuitos concurrentes
// que aislar), alcanza con que AuthTokenStore sea Singleton — así el
// handler y el resto de la app siempre resuelven la misma instancia,
// sin necesidad de construir el HttpClient a mano.
builder.Services.AddSingleton<AuthTokenStore>();
builder.Services.AddTransient<AuthTokenHandler>();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5253/");
})
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<ToastService>();

await builder.Build().RunAsync();
