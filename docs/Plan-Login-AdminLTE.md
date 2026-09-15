# Plan: Login como página de inicio + diseño AdminLTE login.html

## Problema actual
- Login.razor (`/login`) usa `MainLayout` (con sidebar, navbar, footer) — no es el diseño de login.html.
- "/" muestra el dashboard, redirige a `/login` solo DESPUÉS del primer render interactivo (hay flash).
- El body tiene clase `layout-fixed sidebar-expand-lg bg-body-tertiary` que choca con el layout de login.

## Solución propuesta

### 1. Nueva ruta raíz: Login en "/"
- `Login.razor`: rutas `@page "/"` y `@page "/login"`, con `@layout AuthLayout`.
- `Home.razor`: se mueve a `@page "/dashboard"` (solo accesible autenticado).
- NavMenu: el enlace "Dashboard" apunta a `/dashboard`.

### 2. Nuevo layout vacío para autenticación (`AuthLayout.razor`)
```html
<!-- Fondo secundario centrado, sin sidebar ni navbar -->
<div class="d-flex flex-column align-items-center justify-content-center min-vh-100 bg-body-secondary">
    <div class="login-box">
        @Body
    </div>
</div>
```
Reproduce el comportamiento de `.login-page` (CSS de AdminLTE) sin modificar `<body>`.

### 3. Rediseñar `Login.razor` según login.html
Markup inspirado en `D:\wamp64\www\AdminLTE\examples\login.html`:
- `h1.login-logo` con logo y texto "TaskFlow"
- `div.card` > `div.card-body.login-card-body`
- `p.login-box-msg` — "Inicie sesión para comenzar"
- Formulario con `input-group` (email + icono envelope, password + icono lock)
- Checkbox "Recuérdame" + botón "Entrar"
- Toggle a modo registro (nombre, email, contraseña + botón "Crear cuenta")
- Enlace "¿Ya tiene cuenta? / ¿No tiene cuenta?" para cambiar de modo

### 4. AuthenticatedPageBase (se mantiene)
Sigue redirigiendo a `"/"` (login) si el usuario intenta acceder a cualquier `/personas`, `/grupos`, etc. sin token.

### 5. Archivos afectados
| Archivo | Acción |
|---|---|
| `Components/Layout/AuthLayout.razor` | **Crear** — layout vacío para login/register |
| `Components/Pages/Login.razor` | **Reescribir** — rutas "/" y "/login", markup login.html, `@layout AuthLayout` |
| `Components/Pages/Home.razor` | **Editar** — cambiar `@page "/"` a `@page "/dashboard"` |
| `Components/Layout/NavMenu.razor` | **Editar** — Dashboard → `/dashboard` |
| `Components/Layout/MainLayout.razor` | **Editar** — logo brand-link → `/dashboard` |

### 6. Flujo de usuario
```
No autenticado → "/" (login, AuthLayout, sin sidebar)
    ↓ login exitoso
"/dashboard" (MainLayout con sidebar)
    ↓ logout o token expira
"/" (login de nuevo)
```
