# Plan: migrar TaskFlow.Blazor de Blazor Server a Blazor WebAssembly Standalone

**Fecha:** 2026-09-20
**Proyecto:** TaskFlow
**Estado:** Aprobado, en ejecución

## Contexto

El proyecto se construyó como Blazor **Server** (`TaskFlow.Blazor.csproj` con
`Sdk="Microsoft.NET.Sdk.Web"`, `AddInteractiveServerComponents()`,
`AddInteractiveServerRenderMode()`), pero la intención original —confirmada
por escrito en `docs/Plan-Modelo-DB-TaskFlow.md:5`: *"TaskFlow (Blazor
WebAssembly .NET 10 + API .NET 10)..."*— siempre fue Blazor **WebAssembly**
(WASM) como frontend puro en el navegador, hablando con `TaskFlow.Api` como
backend HTTP separado.

Esto explica retroactivamente casi todos los bugs de la sesión anterior: la
fuga de sesión del `AuthTokenStore` singleton, la necesidad de un gate
`Ready` para el orden de carga entre prerender/circuito interactivo, la
reconexión de SignalR fallando tras cada reinicio del servidor, y por qué
nunca aparecían llamadas a la API en la pestaña Network del navegador (en
Server esas llamadas las hace el proceso .NET, no el navegador). La
política CORS que ya existe en `TaskFlow.Api/Program.cs`
(`WithOrigins("https://localhost:7261", "http://localhost:5172")`) es un
vestigio de esa intención original: no tiene ningún efecto en Server (mismo
origen, no aplica CORS), y solo cobra sentido real con un cliente WASM real
llamando desde el navegador.

**Objetivo:** convertir `TaskFlow.Blazor` en un proyecto Blazor WebAssembly
**standalone** (SDK `Microsoft.NET.Sdk.BlazorWebAssembly`), que corre 100%
en el navegador y llama a `TaskFlow.Api` vía `HttpClient` + CORS real —
sin SignalR, sin "circuitos", sin prerender del lado servidor— preservando
todo lo ya construido (autenticación JWT, CRUD de Proyectos con
Versión/Progreso, Flatpickr, dropdown de usuario, validaciones, tema
AdminLTE).

## Por qué Standalone (no el modelo híbrido "Blazor Web App + .Client WASM")

.NET 8+ ofrece dos formas de usar WASM: (a) standalone, un `.csproj` con
`Sdk="Microsoft.NET.Sdk.BlazorWebAssembly"` que compila a estático puro y no
necesita ningún host ASP.NET Core propio, o (b) el modelo híbrido "Blazor
Web App", donde el proyecto principal sigue siendo `Sdk="Microsoft.NET.Sdk.Web"`
(un servidor ASP.NET Core) y un proyecto `.Client` aparte aporta los
componentes que corren en WASM. La documentación original ("Blazor
WebAssembly .NET 10 + API .NET 10", dos mitades claramente separadas) y el
CORS ya configurado (pensado para dos orígenes HTTP distintos) apuntan
inequívocamente a (a): dos aplicaciones independientes, cada una
desplegable por su cuenta.

## Cambios por archivo

### `TaskFlow.Blazor.csproj`
- `Sdk="Microsoft.NET.Sdk.Web"` → `Sdk="Microsoft.NET.Sdk.BlazorWebAssembly"`.
- Se agrega `Microsoft.AspNetCore.Components.WebAssembly` (runtime) y
  `Microsoft.AspNetCore.Components.WebAssembly.DevServer` (`PrivateAssets="all"`,
  solo para `dotnet run` en desarrollo — sirve los estáticos + ejecuta el
  runtime WASM sin necesitar IIS/Kestrel propio).
- Se mantiene el `ProjectReference` a `TaskFlow.Application` (los DTOs se
  siguen compartiendo igual).

### `wwwroot/index.html` (nuevo, reemplaza el rol de documento raíz de `App.razor`)
`index.html` es un archivo **estático puro** — nunca pasa por el motor
Razor, así que no puede contener `@Assets[...]`, `@inject`, etc. Se
reconstruye el `<head>`/`<body>` actual de `App.razor` con paths relativos
directos (sin el helper de Static Web Assets):
```html
<link rel="stylesheet" href="lib/bootstrap/dist/css/bootstrap.min.css" />
<link rel="stylesheet" href="lib/bootstrap-icons/bootstrap-icons.min.css" />
<link rel="stylesheet" href="lib/adminlte/css/adminlte.css" />
<link rel="stylesheet" href="lib/flatpickr/flatpickr.min.css" />
<link rel="stylesheet" href="app.css" />
...
<div id="app">Cargando...</div>
...
<script src="lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
<script src="lib/adminlte/js/adminlte.js"></script>
<script src="lib/flatpickr/flatpickr.min.js"></script>
<script src="lib/flatpickr/l10n/es.js"></script>
<script src="js/theme.js"></script>
<script src="js/validation.js"></script>
<script src="js/notify.js"></script>
<script src="js/datepicker.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
```
El script de theme init inline y el listener `blazor:boot` (reinicializa
AdminLTE) son neutrales — se mantienen igual, el evento `blazor:boot` existe
en ambos hosting models.

### `Components/App.razor` (se simplifica, deja de ser el documento HTML)
Pasa a ser solo el componente raíz montado en `#app`, con el `<Router>`
(vía `<Routes />` o inline) y la llamada a `Theme.InitializeAsync()` en su
`OnAfterRenderAsync` (esto SÍ sigue siendo C#/Razor, se preserva tal cual,
solo que ya no envuelve todo el documento).

### `Program.cs`
Reescritura completa a `WebAssemblyHostBuilder`:
```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<AuthTokenStore>();
builder.Services.AddScoped<AuthTokenHandler>();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5253/");
})
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<ToastService>();

await builder.Build().RunAsync();
```
**Simplificación real, no solo traslado:** el workaround manual de
`HttpClient`/`AuthTokenHandler` que se armó para Blazor Server (construir
el `HttpClient` a mano porque `IHttpClientFactory` resuelve los handlers en
un scope desconectado del circuito — ver `docs/Fix-AuthTokenStore-Scope.md`)
**ya no es necesario en WASM**: no hay circuitos ni scopes-por-conexión, así
que el patrón estándar `AddHttpClient<T>().AddHttpMessageHandler<T>()`
vuelve a funcionar sin el problema que forzó el workaround. Se vuelve a la
forma idiomática.

### `Components/Layout/ReconnectModal.razor` + `ReconnectModal.razor.js` — se eliminan
Son 100% específicos de Blazor Server (`Blazor.reconnect()`,
`Blazor.resumeCircuit()`, concepto de "circuito"). No existen en WASM. Se
borran junto con su referencia `<ReconnectModal />` en `App.razor`.

### `Services/AuthTokenStore.cs`, `AuthState.cs`, `AuthenticatedPageBase.cs`
El gate `Ready`/`TaskCompletionSource` de `AuthTokenStore` y el patrón de
`AuthenticatedPageBase` (chequear auth en `OnAfterRenderAsync`, no en
`OnInitializedAsync`) existen para lidiar con la fase de **prerender**
estático de Server, donde `IJSRuntime` no está disponible todavía. WASM
standalone no prerenderiza — el componente arranca ya con JS interop
disponible desde el primer render. Estrictamente ya no hacen falta, pero
**no se tocan en esta migración**: no rompen nada en WASM (el gate se
completa casi instantáneamente, el patrón de `AuthenticatedPageBase` sigue
siendo funcionalmente correcto, solo deja de ser estrictamente necesario) y
tocarlos agrega riesgo/diff a un cambio ya grande. Se deja como
simplificación opcional a futuro.

### `Components/Shared/FlatpickrDate.razor`
Sin cambios funcionales — `DotNetObjectReference`/`[JSInvokable]`/`IJSRuntime`
funcionan igual en WASM. El `catch (JSDisconnectedException)` en
`DisposeAsync` queda inofensivo (casi nunca se dispara en WASM, no hay
reconexión de red), no hace falta quitarlo.

### `TaskFlow.Api/Program.cs` (CORS)
El middleware ya está en el orden correcto (`UseCors` antes de
`UseAuthentication`/`UseAuthorization`). Solo hay que confirmar que
`WithOrigins(...)` liste el/los orígenes reales donde corra el WASM (puerto
del nuevo `dotnet run`/dev server de `TaskFlow.Blazor`). Con JWT Bearer (no
cookies) no hace falta `AllowCredentials()`.

### `Properties/launchSettings.json` de `TaskFlow.Blazor`
Se ajusta al perfil de WASM (el dev server de WASM sirve los estáticos +
runtime; puertos pueden mantenerse iguales a los actuales — 5172/7261— para
no tener que tocar el CORS de la API).

## Qué NO cambia

`Services/ApiClient.cs` (HttpClient tipado puro), `ToastService.cs`,
`ThemeState.cs`, `Routes.razor`, `MainLayout.razor`, `NavMenu.razor`,
`AuthLayout.razor`, y **todas las páginas** (`Home.razor`, `Personas.razor`,
`Grupos.razor`, `Periodos.razor`, `Tareas.razor`, `Import.razor`,
`Configuracion.razor`, `Login.razor`) — son Razor/C# estándar sin
dependencias del hosting model, confirmado en la exploración. El dominio,
la aplicación, la infraestructura y la API (`TaskFlow.Domain/Application/
Infrastructure/Api`) no se tocan en absoluto.

## Orden de ejecución

1. `TaskFlow.Blazor.csproj`: cambiar SDK y paquetes.
2. Crear `wwwroot/index.html`; simplificar `App.razor` a componente raíz.
3. Reescribir `Program.cs` (WASM host builder + simplificar el registro de
   `HttpClient`/`AuthTokenHandler`).
4. Eliminar `ReconnectModal.razor` + `.js` y su referencia.
5. Ajustar `launchSettings.json` de `TaskFlow.Blazor`.
6. Ajustar CORS en `TaskFlow.Api/Program.cs` si el puerto cambia.
7. Build de `TaskFlow.Blazor` como proyecto WASM (`dotnet build`,
   `dotnet run` con el dev server).
8. Prueba manual real en navegador: login, ver el listado de Proyectos, y
   confirmar en la pestaña Network que **ahora sí aparecen** las llamadas a
   `http://localhost:5253/api/...` directamente desde el navegador (la
   prueba definitiva de que la migración funcionó).

## Verificación

- Build de `TaskFlow.slnx` completo sin errores.
- `dotnet run --project TaskFlow.Api` + `dotnet run --project TaskFlow.Blazor`
  (el segundo ahora es el dev server de WASM).
- Abrir el navegador, loguearse, y en DevTools → Network confirmar
  peticiones reales a la API (esto es justo lo que hoy NO se veía, y es la
  prueba de que dejó de ser Server).
- Repetir el flujo de Proyectos (crear/editar con Versión/Progreso,
  Flatpickr en los 3 campos de fecha, dropdown de usuario) para confirmar
  que nada se rompió al cambiar el hosting model.
- No hay navegador headless disponible en este entorno de trabajo (ya se
  intentó Playwright antes y falló la descarga del binario) — la prueba
  visual final la tiene que hacer el usuario en su propio navegador; se
  puede verificar build + que la API responda correctamente a peticiones
  `curl` con encabezado `Origin` simulando el navegador (para confirmar que
  CORS responde bien), pero no el comportamiento visual real del WASM.

## Evidencia encontrada durante la investigación

- Única mención de "WebAssembly" en todo el repositorio:
  `docs/Plan-Modelo-DB-TaskFlow.md:5`.
- `TaskFlow-Architecture.md`, `AGENTS.md`, `TaskFlow.txt` y el resto de
  `docs/` describen la UI como "Blazor" genérico, sin especificar Server vs
  WASM.
- Ningún commit del historial (`git log --all --oneline`) menciona WASM,
  "standalone" ni un cambio de arquitectura del frontend.
- El CORS de `TaskFlow.Api/Program.cs` apuntando a orígenes fijos de
  navegador es la pieza de evidencia más concreta: no tiene ningún efecto
  en Blazor Server (mismo origen), y solo tiene sentido para un cliente que
  llame desde el navegador — exactamente lo que hace WASM.
