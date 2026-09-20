# Fix: dropdown de usuario angosto y 401 al autenticarse

**Fecha:** 2026-09-20
**Proyecto:** TaskFlow
**Estado:** Corregido y verificado

## Problema 1: los botones del footer del dropdown de usuario no entraban

### Síntoma

En el dropdown de usuario del header (`MainLayout.razor`), los botones
"Configuración" y "Cerrar sesión" del `user-footer` no entraban en el ancho
del menú desplegable (`ul.dropdown-menu.dropdown-menu-lg.dropdown-menu-end`).

### Causa raíz

La clase `dropdown-menu-lg` de Bootstrap 5 solo define una variable CSS
(`--bs-dropdown-min-width`), pero AdminLTE tiene una regla más específica
que fija un ancho fijo, no un mínimo:

```css
.navbar-nav > .user-menu > .dropdown-menu {
  width: 280px;
  padding: 0;
}
```

Un `width` fijo por selector de clase gana sobre el `min-width` que
`dropdown-menu-lg` intenta aplicar, así que el menú se quedaba en 280px sin
importar la clase de Bootstrap.

### Solución aplicada

Se agregó un estilo inline al `<ul>` del dropdown en `MainLayout.razor`, que
por especificidad siempre gana sobre una regla de clase:

```html
<ul class="dropdown-menu dropdown-menu-lg dropdown-menu-end" style="width: 320px">
```

**Actualización:** ese primer intento no cambiaba nada visualmente. La causa
real es otra regla, también en AdminLTE, que define `.dropdown-menu-lg`
directamente (no es la utilidad de tamaño de Bootstrap, que solo define una
variable CSS):

```css
.dropdown-menu-lg {
  min-width: 280px;
  max-width: 300px;
  padding: 0;
}
```

`max-width` es una propiedad CSS **distinta** de `width`: un `style="width: 320px"`
inline gana la pulseada por la propiedad `width`, pero no toca `max-width`, así
que el navegador seguía recortando la caja a 300px. Hubo que anular también
`max-width` en el inline style:

```html
<ul class="dropdown-menu dropdown-menu-lg dropdown-menu-end" style="width: 320px; max-width: 320px">
```

## Problema 2: toast "401 (Unauthorized)" justo después de iniciar sesión

### Síntoma

Al loguearse, la página `/dashboard` mostraba un toast de error con
`Response status code does not indicate success: 401 (Unauthorized).`

### Causa raíz

En `Login.razor`, tanto `LoginAsync` como `RegisterAsync` navegaban así tras
autenticar:

```csharp
Nav.NavigateTo("/dashboard", forceLoad: true);
```

`forceLoad: true` fuerza una recarga completa del navegador, lo que **cierra
el circuito de Blazor Server actual y abre uno nuevo**. Ese circuito nuevo
arranca con `AuthTokenStore` vacío — es `scoped` desde el fix anterior
(`docs/Fix-AuthTokenStore-Scope.md`), así que cada circuito tiene su propia
instancia sin el token en memoria.

El token recién se restaura desde `localStorage` en
`AuthState.LoadAsync()`, que corre en `AuthenticatedPageBase.OnAfterRenderAsync`
— y eso pasa **después** de que `Home.razor` ya intentó pedir los proyectos
en su propio `OnInitializedAsync`. Es la misma carrera de inicialización que
ya se había documentado como riesgo pendiente en el fix anterior, y en este
caso se disparaba siempre, porque el `forceLoad` garantizaba un circuito
nuevo (sin token) en cada login.

### Solución aplicada

Se quitó `forceLoad: true` en ambos lugares de `Login.razor`:

```csharp
Nav.NavigateTo("/dashboard");
```

Al ser una navegación normal dentro del **mismo circuito**, `AuthTokenStore`
ya tiene el token que `AuthState.LoginAsync()` acaba de guardar en memoria
(antes de escribirlo en `localStorage`), así que `Home.razor` lo usa
correctamente desde el primer pedido, sin depender de restaurarlo por JS
interop.

### Riesgo relacionado, confirmado luego: pasaba también en un hard refresh

Quitar el `forceLoad` resolvía la carrera puntualmente en el momento de
login/registro, pero el riesgo que había quedado documentado como pendiente
se confirmó: haciendo un hard refresh (F5) sobre una página ya autenticada
aparecía el mismo error 401. Un F5 también crea un circuito de Blazor nuevo
(sin pasar por `Login.razor`), así que la misma carrera entre
`OnInitializedAsync` de cada página y `AuthState.LoadAsync()` se repetía.

### Solución de fondo (segunda vuelta)

En vez de corregir el orden de carga en cada página (`AuthenticatedPageBase`
y las 7 páginas que heredan de ella), se centralizó el fix en el punto donde
realmente importa: justo antes de que salga cualquier pedido HTTP hacia la
API.

- `AuthTokenStore.cs`: expone un gate `Ready` (`Task`, respaldado por un
  `TaskCompletionSource`) que se completa una sola vez, cuando la sesión de
  este circuito ya fue confirmada.
- `AuthState.cs`: marca ese gate (`_tokenStore.MarkReady()`) al final de
  `LoadAsync()` (haya o no restaurado un token) y también apenas se aplica un
  login/registro exitoso — necesario porque `/login` usa `AuthLayout`, no
  `MainLayout`, así que `LoadAsync()` nunca corre antes de loguearse.
- `AuthTokenHandler.cs`: antes de adjuntar el header y mandar el pedido,
  espera `_tokenStore.Ready` (con un timeout de 5s para no colgarse si algo
  falla) en su `SendAsync`.

Como `AuthTokenStore` es `scoped` (una instancia por circuito) y
`AuthTokenHandler` es el único punto por el que pasa toda llamada de
`ApiClient`, esto garantiza que ningún pedido salga hacia la API hasta saber
si hay sesión o no, sin importar en qué orden Blazor dispare los
`OnInitializedAsync` del layout y de la página. Se mantiene la eliminación
del `forceLoad` en `Login.razor` (navegación más simple, sin recarga
completa), pero el fix que realmente cierra el problema es este gate.

## Archivos afectados

| Archivo | Acción |
|---|---|
| `docs/Fix-Dropdown-Ancho-Y-401-Login.md` | nuevo (este documento) |
| `TaskFlow.Blazor/Components/Layout/MainLayout.razor` | editado (ancho del dropdown de usuario) |
| `TaskFlow.Blazor/Components/Pages/Login.razor` | editado (se quita `forceLoad: true` en login y registro) |
| `TaskFlow.Blazor/Services/AuthTokenStore.cs` | editado (gate `Ready`/`MarkReady()`) |
| `TaskFlow.Blazor/Services/AuthState.cs` | editado (marca el gate en `LoadAsync()` y `Apply()`) |
| `TaskFlow.Blazor/Services/AuthTokenHandler.cs` | editado (espera el gate antes de cada pedido) |
