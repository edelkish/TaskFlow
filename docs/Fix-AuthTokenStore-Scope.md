# Fix: fuga de sesión entre usuarios por lifetime incorrecto de `AuthTokenStore`

**Fecha:** 2026-09-20
**Proyecto:** TaskFlow
**Estado:** Corregido y verificado

## Síntoma

En `TaskFlow.Blazor`, una pestaña/usuario sin sesión iniciada podía ver contenido de
otra sesión ya autenticada en el mismo servidor. Se detectó al probar la página
`/dashboard` con una petición HTTP directa (sin login): mostraba el email y los
datos de un usuario admin que estaba logueado en otra conexión.

## Causa raíz

### 1. `AuthTokenStore` registrado como `Singleton`

En `TaskFlow.Blazor/Program.cs`:

```csharp
builder.Services.AddSingleton<AuthTokenStore>();   // ← una sola instancia para TODO el proceso
builder.Services.AddScoped<AuthState>();            // ← una instancia por circuito (correcto)
```

`AuthTokenStore` guarda el JWT (`Token`) y el `Email` de la sesión. Al ser
`Singleton`, **una sola instancia se comparte entre todos los circuitos de Blazor
Server** (cada circuito = una pestaña/usuario conectado). El primer usuario que
inicia sesión deja el token guardado ahí para siempre, y cualquier otro
circuito —incluida una petición sin login— lo reutiliza sin darse cuenta.

`AuthState` (que expone `Roles` e `IsAdmin`), en cambio, sí estaba correctamente
declarado como `Scoped`, por lo que quedaba una mezcla inconsistente: sesión
"autenticada" (por el token del singleton) pero sin roles cargados en ese
circuito nuevo.

### 2. Por qué no basta con cambiar `Singleton` → `Scoped`

El `HttpClient` de `ApiClient` se registraba así:

```csharp
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5253/");
})
    .AddHttpMessageHandler<AuthTokenHandler>();
```

`AddHttpMessageHandler<T>` hace que `IHttpClientFactory` construya y **cachee/recicle**
el handler (`AuthTokenHandler`) en un **scope interno propio del factory**
(pensado para poder rotar handlers cada ~2 minutos por defecto), que está
**desconectado del scope del circuito de Blazor Server**.

Si solo se cambia `AuthTokenStore` a `Scoped` sin tocar esto, `AuthTokenHandler`
seguiría resolviendo su `AuthTokenStore` desde ese scope interno del factory —no
el del usuario real— y el bug de sesión compartida seguiría existiendo, solo que
de forma más errática (la instancia compartida rotaría cada par de minutos en
vez de persistir para siempre).

## Solución aplicada

En `TaskFlow.Blazor/Program.cs`:

```csharp
// AuthTokenStore y el HttpClient de ApiClient son scoped (uno por
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
```

- `AuthTokenStore`: `Singleton` → `Scoped`.
- Se dejó de usar `AddHttpClient<ApiClient>()` + `AddHttpMessageHandler<AuthTokenHandler>()`.
  En su lugar, el `HttpClient` se construye manualmente encadenando
  `AuthTokenHandler` como su `InnerHandler`, resuelto dentro del **mismo** scope
  que `AuthTokenStore` y `AuthState`.
- `ApiClient` pasa de transient (vía `AddHttpClient`) a `Scoped`.
- No fue necesario tocar `ApiClient.cs` ni `AuthTokenHandler.cs`, solo el cableado
  de DI.

Se pierde el *connection pooling*/reciclado de handlers que da
`IHttpClientFactory`, pero es una app interna que solo habla con una API local:
compensación razonable a cambio de no filtrar tokens entre usuarios.

## Verificación

- Build de `TaskFlow.Blazor` sin errores.
- Antes del fix: una petición sin login a `/dashboard` devolvía el email y los
  datos del usuario admin logueado en otra conexión.
- Después del fix: tres peticiones independientes seguidas a `/dashboard`
  (sin login) muestran consistentemente el enlace "Iniciar sesión" — ya no hay
  fuga de sesión entre conexiones.

## Riesgo relacionado, pendiente (no corregido en este cambio)

El diseño anterior (singleton) tapaba sin querer un problema de orden de carga
en las páginas:

- Las páginas piden sus datos en `OnInitializedAsync` (ej. `Home.razor` llama a
  `Api.GetProjectsAsync()`).
- `AuthState.LoadAsync()` —que restaura el token desde `localStorage`— corre en
  `AuthenticatedPageBase.OnAfterRenderAsync`, que se ejecuta **después** de
  `OnInitializedAsync`.

Con el singleton esto no se notaba: el token ya estaba poblado globalmente en
cuanto alguien se logueaba una vez, sin importar el orden por circuito. Con
`AuthTokenStore` ahora `Scoped`, cada circuito nuevo arranca "en blanco", así
que es posible que la primera carga de una página tras un login o F5 muestre
brevemente "sin datos" hasta que el token se restaure en ese circuito.

Si se quiere cerrar esto, la corrección consistiría en mover la carga de
`AuthState`/el chequeo de redirección a `OnInitializedAsync` de
`AuthenticatedPageBase` (para que corra antes que el fetch de datos de cada
página), lo cual requiere que cada página que hereda de `AuthenticatedPageBase`
llame a `base.OnInitializedAsync()` antes de su propia lógica.

## Archivos afectados

| Archivo | Acción |
|---|---|
| `docs/Fix-AuthTokenStore-Scope.md` | nuevo (este documento) |
| `TaskFlow.Blazor/Program.cs` | editado (lifetimes de DI de `AuthTokenStore`, `HttpClient` y `ApiClient`) |
