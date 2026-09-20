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

### Riesgo relacionado, todavía pendiente

Esto resuelve la carrera específicamente para el momento de login/registro.
La misma causa de fondo (orden entre `OnInitializedAsync` de cada página y
`AuthState.LoadAsync()` en `OnAfterRenderAsync`) podría repetirse en otros
escenarios que sí crean un circuito nuevo sin pasar por login, por ejemplo
un F5 (recarga manual) sobre una página ya autenticada. No se corrigió aquí
porque no fue el síntoma reportado; requeriría revisar el orden de carga en
`AuthenticatedPageBase` y en cada página que hereda de ella.

## Archivos afectados

| Archivo | Acción |
|---|---|
| `docs/Fix-Dropdown-Ancho-Y-401-Login.md` | nuevo (este documento) |
| `TaskFlow.Blazor/Components/Layout/MainLayout.razor` | editado (ancho del dropdown de usuario) |
| `TaskFlow.Blazor/Components/Pages/Login.razor` | editado (se quita `forceLoad: true` en login y registro) |
