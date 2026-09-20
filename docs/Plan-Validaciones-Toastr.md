# Plan: Validaciones (Custom Validation AdminLTE) + Notificaciones (toastr)

**Fecha:** 2026-09-15
**Proyecto:** TaskFlow
**Estado:** Aprobado y en ejecución

## Contexto

- La autenticación ya está completa (JWT con roles, todos los controladores `[Authorize]`,
  integración UI↔API vía `AuthTokenHandler` y roles en `AuthResponseDto`). No requiere cambios.
- Las pantallas actuales solo muestran `alert-danger` con textos manuales; **no** existe la
  validación nativa "Custom Validation" de `validation.html` de AdminLTE.
- **toastr NO está instalado** (y tampoco jQuery). AdminLTE 4 es una plantilla sin jQuery.

## Objetivos

1. Aplicar en todo el site las **Custom Validation** de `D:\wamp64\www\AdminLTE\forms\validation.html`
   (validación nativa de Bootstrap 5).
2. Instalar y emplear **toastr** para notificaciones de error y success; si la descarga
   desde CDN falla (máquina offline), **fallback a los Toasts nativos de Bootstrap 5**.

## Decisiones acordadas

- Reemplazar los banners `alert-danger` por notificaciones (la validación de campo queda
  inline con `invalid-feedback`).
- Vendor local: `jquery.min.js` (3.7.1) y `toastr.min.js` + `toastr.min.css` (2.1.4)
  descargados desde CDN hacia `wwwroot/lib/`.
- **IMPORTANTE (resultado):** la máquina NO tiene salida a internet (fallaron
  code.jquery.com, cdnjs, unpkg y jsdelivr). Se aplica el **fallback acordado: Toasts
  nativos de Bootstrap 5** (ya incluidos en `bootstrap.bundle.min.js`). El wrapper
  `notify.js` sigue siendo compatible con toastr (si se detecta `window.toastr`, lo usa).

## Patrón de "Custom Validation" a portar (validation.html)

```html
<form class="needs-validation" novalidate>
  <input required ... />
  <div class="invalid-feedback">Mensaje...</div>
  <div class="valid-feedback">Correcto.</div>
</form>
```

Script de la plantilla: en `submit`, si `!form.checkValidity()` → `preventDefault` +
`stopPropagation`; siempre añadir `was-validated`.

Para Blazor (incluidas filas de edición dinámicas), se expone
`window.taskflow.validateForm(form)` que devuelve `true`/`false` y añade `was-validated`
sobre fallo; cada handler de envío lo invoca ANTES de llamar al API.

## Pasos de trabajo

### 1. Vendor + referencias
- Descargar a `TaskFlow.Blazor/wwwroot/lib/`:
  - `jquery/jquery.min.js` (jQuery 3.7.1)
  - `toastr/toastr.min.js` + `toastr/toastr.min.css` (toastr 2.1.4)
- **Fallback** si falla: notificaciones con Bootstrap 5 Toasts (css/js ya incluidos en el bundle).
- `App.razor`:
  - `<link href="lib/toastr/toastr.min.css" ...>` en `<head>` (solo si existe vendor).
  - Scripts antes de `blazor.web.js`: `jquery.min.js`, `toastr.min.js`, `js/notify.js`, `js/validation.js`.

### 2. Infraestructura JS + servicio Blazor
- `wwwroot/js/validation.js` — `window.taskflow.validateForm(form)`:
  - Si `form.checkValidity()` → `true`.
  - Si no → `form.classList.add('was-validated')` → `false`.
- `wwwroot/js/notify.js` — `window.taskflow.toast.{success,error,info,warning}(message)`:
  - Usa toastr (si `window.toastr`) con `positionClass:'toast-top-right'`, progressBar,
    closeButton, timeOut ≈ 3000ms.
  - Si no hay toastr, crea un Bootstrap Toast (`.toast`) con la clase según tipo.
- `Services/ToastService.cs` — servicio scoped que envuelve IJSRuntime:
  - `Error(message)`, `Success(message)`, `Info(message)`, `Warning(message)`.
- `Program.cs` (Blazor): `builder.Services.AddScoped<ToastService>();`.

### 3. Aplicar validación + reemplazar banners por pantalla
- **Login.razor**: formularios login/registro con `needs-validation`, campos `required`,
  feedbacks y toasts de error/success.
- **Personas.razor**: crear/editar (nombre obligatorio) + toasts de éxito/error.
- **Grupos.razor**: crear grupo (periodo obligatorio) + toasts.
- **Periodos.razor**: crear periodo (año obligatorio) + toasts.
- **Tareas.razor**: crear/editar tarea (descripción y nº obligatorios) + toasts.
- **Import.razor**: ruta TXT obligatoria; errores/success vía toast; advertencias del
  resultado siguen visibles en pantalla.
- **Configuracion.razor**: hex de color obligatorio/formato + toasts de éxito/error.
- Se eliminan todos los bloques `@if (Error != null) { <div class="alert alert-danger">... }`.

### 4. Verificación
- Build de los 3 proyectos (TaskFlow.Infrastructure, TaskFlow.Api, TaskFlow.Blazor).
- Arranque API (http://localhost:5253) y Blazor; checklist manual:
  - Login fallido → toast de error; login admin → redirect a dashboard.
  - Crear/editar persona y periodo → validación inline + toast success.
  - Guardar tema (Configuración, solo admin) → toast success.
  - Importar ruta vacía → toast error; ruta válida → toast success con resumen.
- Commit + push a `main`.

## Archivos afectados

| Archivo | Acción |
|---|---|
| `docs/Plan-Validaciones-Toastr.md` | nuevo (este documento) |
| `TaskFlow.Blazor/wwwroot/lib/jquery/jquery.min.js` | nuevo (vendor) |
| `TaskFlow.Blazor/wwwroot/lib/toastr/toastr.min.css` | nuevo (vendor) |
| `TaskFlow.Blazor/wwwroot/lib/toastr/toastr.min.js` | nuevo (vendor) |
| `TaskFlow.Blazor/wwwroot/js/validation.js` | nuevo |
| `TaskFlow.Blazor/wwwroot/js/notify.js` | nuevo |
| `TaskFlow.Blazor/Services/ToastService.cs` | nuevo |
| `TaskFlow.Blazor/Components/App.razor` | editar (css/js) |
| `TaskFlow.Blazor/Program.cs` | editar (registrar ToastService) |
| `Components/Pages/Login.razor` | editar (validación + toasts) |
| `Components/Pages/Personas.razor` | editar (validación + toasts) |
| `Components/Pages/Grupos.razor` | editar (validación + toasts) |
| `Components/Pages/Periodos.razor` | editar (validación + toasts) |
| `Components/Pages/Tareas.razor` | editar (validación + toasts) |
| `Components/Pages/Import.razor` | editar (validación + toasts) |
| `Components/Pages/Configuracion.razor` | editar (validación + toasts) |