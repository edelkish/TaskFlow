# Plan: date-picker Flatpickr + columnas Versión/Progreso en Proyectos

**Fecha:** 2026-09-20
**Proyecto:** TaskFlow
**Estado:** Implementado y verificado (Feature 2 end-to-end contra la API real; Feature 1 pendiente de prueba visual manual)

## Contexto

Se pidió instalar `eonasdan-bootstrap-datetimepicker` para los campos de
fecha del listado de Proyectos (`Home.razor`), y agregar dos columnas
nuevas: "Versión" y "Progreso" (este último como barra de progreso, igual a
`pages/projects.html` de la plantilla de referencia AdminLTE).

## Decisiones acordadas

- **Date-picker: Flatpickr 4.6.13, no eonasdan-bootstrap-datetimepicker.**
  El plugin pedido es jQuery + Moment.js, pensado para Bootstrap 3/4.
  Investigando `C:\wamp64\www\AdminLTE\forms\advanced.html` se confirmó que
  la plantilla de referencia ya instalada usa Flatpickr 4.6.13 (vanilla JS,
  sin jQuery) para sus date-pickers — coherente con la decisión previa de
  no usar jQuery en este proyecto (`docs/Plan-Validaciones-Toastr.md`).
- **Progreso: campo manual editable (0-100), no calculado.** No hay ningún
  concepto de "tarea completada" en el dominio (`PlanningTask` no tiene
  estado de completitud), así que calcularlo habría sido un feature mucho
  más grande. Se agrega como dato simple, igual que Versión.

## Feature 2: columnas "Versión" y "Progreso"

### Cambios

| Capa | Archivo | Cambio |
|---|---|---|
| Domain | `TaskFlow.Domain/Entities/Project.cs` | `Version` (`string?`), `Progress` (`int`, default 0) |
| Infrastructure | `TaskFlow.Infrastructure/Data/Configurations/ProjectConfiguration.cs` | `HasMaxLength(50)` en Version; `Progress` requerido con default 0; `CHECK (Progress BETWEEN 0 AND 100)` a nivel de tabla |
| Infrastructure | Migración `AddProjectVersionAndProgress` | agrega las 2 columnas + el check constraint |
| Application | `TaskFlow.Application/DTOs/ProjectDto.cs` | `Version`/`Progress` en `ProjectDto`, `CreateProjectDto`, `UpdateProjectDto` |
| Application | `TaskFlow.Application/Validators/CreateProjectValidator.cs` | `MaximumLength(50)` / `InclusiveBetween(0, 100)` |
| Application | `TaskFlow.Application/Services/ProjectService.cs` | ver "hallazgo crítico" abajo |
| Blazor | `TaskFlow.Blazor/Components/Pages/Home.razor` | inputs de alta/edición, columnas de tabla, barra de progreso |

### Hallazgo crítico durante la implementación

`ProjectService.UpdateAsync` **no usa AutoMapper** — asigna los campos del
DTO a la entidad a mano (`project.Name = dto.Name; ...`). Si solo se
agregaban `Version`/`Progress` al DTO sin tocar este método, el `PUT`
habría devuelto 200 pero el valor nunca se habría guardado (bug silencioso,
sin excepción). Se agregó ahí mismo `project.Version = dto.Version;` y
`project.Progress = Math.Clamp(dto.Progress, 0, 100);` (el mismo clamp se
aplica en `CreateAsync`, ya que la API es la última línea de defensa real
del rango — la app no tiene wireado el auto-validation de FluentValidation
en el pipeline HTTP, así que lo que realmente valida hoy es el HTML nativo
del navegador más este clamp del lado del servidor).

### Barra de progreso

Mismo markup que `pages/projects.html` de la plantilla:
```html
<div class="d-flex align-items-center gap-2">
    <div class="progress flex-grow-1" style="height: 6px">
        <div class="progress-bar bg-primary" role="progressbar"
             style="width: 40%" aria-valuenow="40"
             aria-valuemin="0" aria-valuemax="100"></div>
    </div>
    <small class="text-secondary" style="min-width: 2.25rem">40%</small>
</div>
```
El color de la barra varía según el %, con un helper `ProgressBarClass` en
`Home.razor` (`>=100` verde, `>=60` azul, `>=30` celeste, resto amarillo) —
la plantilla de referencia también varía el color por proyecto pero sin una
regla fija documentada, así que esta es una decisión propia, coherente con
los badges Activo/Finalizado que ya existían en la página.

### Verificación

Contra la API real (`curl`, mismo patrón que fixes previos): login → `POST`
con `version`/`progress` → `GET` (confirma creación) → `PUT` cambiando
ambos campos → `GET` de nuevo confirmando que cambiaron (esto es justo lo
que detecta si el fix de `UpdateAsync` quedó bien aplicado) → `PUT` con
`progress: 150` confirmando que el servidor lo clampea a `100`. Todo
verificado exitosamente. Se confirmó también por `sqlcmd` que la migración
aplicó las columnas con los tipos/constraints esperados.

## Feature 1: Flatpickr 4.6.13

### Vendoring

Descargados y colocados a mano en `TaskFlow.Blazor/wwwroot/lib/flatpickr/`
(mismo patrón manual que `adminlte/`, `bootstrap/`, sin build tool):
`flatpickr.min.css`, `flatpickr.min.js`, `l10n/es.js` (locale español, ya
que el resto de la UI está en español). Se verificó el hash SHA-384 de
`flatpickr.min.css`/`flatpickr.min.js` contra el atributo `integrity` que
usa la plantilla de referencia — coinciden exactamente, confirmando que son
los mismos archivos.

### Mecanismo de sincronización JS↔Blazor

Flatpickr no dispara de forma confiable el evento nativo `change`/`input`
del DOM al elegir una fecha por calendario, así que **no** se puede usar
`@bind` normal sobre el input. Se usa en cambio el callback `onChange` de
Flatpickr (que sí es siempre confiable), conectado a C# vía `[JSInvokable]`
+ `DotNetObjectReference` — primer uso de este mecanismo en el proyecto
(los wrappers JS existentes, `notify.js`/`validation.js`, son solo C#→JS).

**Decisión importante:** el input **no** se marca `readonly`. Por spec
HTML5, un input `readonly` queda exento de la validación `required` del
navegador — como el campo "Fecha inicio" depende de esa validación nativa
(`class="needs-validation"`), usar `readonly` la habría roto en silencio.
En su lugar se usa `allowInput: true` en Flatpickr sobre un
`<input type="text">` normal, que preserva `required` y sigue disparando
`onChange` de forma confiable al elegir del calendario.

### Componente `FlatpickrDate.razor`

En vez de cablear `ElementReference` + `DotNetObjectReference` +
`[JSInvokable]` + lógica de "no reinicializar de más" tres veces a mano en
`Home.razor` (los 3 campos de fecha son casi idénticos), se encapsuló en
`TaskFlow.Blazor/Components/Shared/FlatpickrDate.razor`, un componente con
two-way binding (`@bind-Value`) que aprovecha el ciclo de vida propio de
Blazor: al montarse (`OnAfterRenderAsync(firstRender: true)`) inicializa
Flatpickr una sola vez; al desmontarse (`IAsyncDisposable.DisposeAsync`,
disparado automáticamente cuando el `@if` que lo envuelve en `Home.razor`
se vuelve falso, o cuando cambia la fila en edición) destruye la instancia
y libera el `DotNetObjectReference`. Esto evita tener que rastrear a mano
en `Home.razor` si un picker ya fue inicializado o si cambió de fila.

`Home.razor` usa el componente así:
```html
<FlatpickrDate @bind-Value="NewStartDate" Required="true" />
<FlatpickrDate @bind-Value="NewEndDate" />
<FlatpickrDate @bind-Value="EditEndDate" CssClass="form-control form-control-sm" />
```
(`NewStartDate` pasó de `DateTime` a `DateTime?` para poder usar el mismo
componente en los 3 campos; `CreateAsync` usa `NewStartDate ?? DateTime.Today`
al construir el DTO).

### Verificación

Sin navegador headless disponible en este entorno (Playwright se intentó en
una sesión anterior y el binario de Chromium no se pudo descargar por
restricción de red al CDN de Playwright específicamente), la verificación
se limitó a lo comprobable sin navegador:
- Build completo de la solución sin errores.
- Hash SHA-384 de los archivos vendorizados coincide con el `integrity` de
  la plantilla de referencia.
- `curl http://localhost:5172/dashboard` confirma que el HTML servido
  incluye los `<link>`/`<script>` de Flatpickr (con fingerprint de
  `@Assets[...]`, ej. `lib/flatpickr/flatpickr.min.i8fj6rnb82.js`) y
  `js/datepicker.js`, y que las columnas "Versión"/"Progreso" están en la
  tabla.
- No se pudo verificar visualmente que el calendario abra ni que no se
  dupliquen instancias al abrir/cerrar el alta o cambiar de fila en
  edición — **queda pendiente de prueba manual del usuario en su propio
  navegador.**

## Archivos afectados

| Archivo | Feature | Acción |
|---|---|---|
| `TaskFlow.Domain/Entities/Project.cs` | 2 | editado |
| `TaskFlow.Infrastructure/Data/Configurations/ProjectConfiguration.cs` | 2 | editado |
| `TaskFlow.Infrastructure/Migrations/20260920101132_AddProjectVersionAndProgress.cs` (+Designer, snapshot) | 2 | nuevo |
| `TaskFlow.Application/DTOs/ProjectDto.cs` | 2 | editado |
| `TaskFlow.Application/Services/ProjectService.cs` | 2 | editado (fix crítico en `UpdateAsync`) |
| `TaskFlow.Application/Validators/CreateProjectValidator.cs` | 2 | editado |
| `TaskFlow.Blazor/Components/Pages/Home.razor` | 1 y 2 | editado |
| `TaskFlow.Blazor/wwwroot/lib/flatpickr/flatpickr.min.css` | 1 | nuevo (vendor) |
| `TaskFlow.Blazor/wwwroot/lib/flatpickr/flatpickr.min.js` | 1 | nuevo (vendor) |
| `TaskFlow.Blazor/wwwroot/lib/flatpickr/l10n/es.js` | 1 | nuevo (vendor) |
| `TaskFlow.Blazor/wwwroot/js/datepicker.js` | 1 | nuevo |
| `TaskFlow.Blazor/Components/Shared/FlatpickrDate.razor` | 1 | nuevo |
| `TaskFlow.Blazor/Components/App.razor` | 1 | editado |
| `TaskFlow.Blazor/Components/_Imports.razor` | 1 | editado (`@using` del namespace `Shared`) |
| `docs/Plan-Flatpickr-Version-Progreso.md` | 1 y 2 | nuevo (este documento) |
