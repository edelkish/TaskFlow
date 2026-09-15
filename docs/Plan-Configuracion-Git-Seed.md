# Plan: Git, Configuraciones del Sistema (Theme) y Seed de Usuarios

Fecha: 2026-09-15
Estado: Aprobado por el usuario (pendiente de implementación)

## Contexto

- El repositorio `E:\Proyectos\Generacion\TaskFlow` no es un repositorio git todavía.
- Se pide persistir en BD las configuraciones del sistema (en concreto el theme a emplear),
  tomando como referencia el generador `D:\wamp64\www\AdminLTE\generate\theme.html` de AdminLTE
  4 (un "theme" en AdminLTE 4 = color mode + utilidades de fondo `bg-*` + color primario).
- Se pide sembrar un usuario administrador con acceso total y un usuario por cada rol del sistema.

## Decisiones tomadas

- Push a GitHub: se prueba con Git Credential Manager (no hay `gh` CLI instalado).
- `appsettings.json` (ConnectionString sa/sa + Secret JWT) se sube tal cual: es entorno de desarrollo local.
- Passwords de los usuarios por rol: `@User123/*-+`. Admin: `@Admin123/*-+` (indicada por el usuario).
- Rama por defecto de git: `main`.
- Remote: `https://github.com/edelkish/TaskFlow.git`

---

## 1. Git — inicializar repo y push

1. `git init -b main` en la raíz del repo.
2. Crear `.gitignore` con `bin/`, `obj/`, `.vs/`, `*.user` y artefactos de publicación.
3. `git add -A` + commit inicial.
4. `git remote add origin https://github.com/edelkish/TaskFlow.git` + `git push -u origin main`.

## 2. Configuraciones del sistema (solo básico: theme)

Siguiendo Clean Architecture y el patrón existente de Personas/Períodos.

### Modificaciones por capa

| Capa | Archivos | Contenido |
|------|----------|-----------|
| Domain | `TaskFlow.Domain/Entities/AppSetting.cs` | Entidad `AppSetting : BaseEntity` (`Key`, `Value`, `Description?`) |
| Domain | `TaskFlow.Domain/Interfaces/IAppSettingRepository.cs`, `IUnitOfWork.cs` | Repositorio con `GetByKeyAsync` + propiedad en `IUnitOfWork` |
| Infra | `TaskFlow.Infrastructure/Repositories/AppSettingRepository.cs`, `UnitOfWork.cs` | Implementación sobre `GenericRepository<AppSetting>` |
| Infra | `TaskFlow.Infrastructure/Data/TaskFlowDbContext.cs` | `DbSet<AppSetting>` + configuración (índice único en `Key`) |
| Infra | Migración nueva `AddAppSettings` | `dotnet ef migrations add AddAppSettings` |
| Infra | `TaskFlow.Infrastructure/DependencyInjection.cs` | Registro del repo/servicio |
| Application | `Services/SettingsService.cs`, `Interfaces/ISettingsService.cs`, DTOs | `ThemeSettingsDto` + `GetThemeAsync` + `UpsertThemeAsync` |
| API | `TaskFlow.Api/Controllers/SettingsController.cs` | `GET api/settings` (sin auth, aplica en login) y `PUT api/settings` con `[Authorize(Roles="Admin")]` |
| Blazor | `Services/SettingsClient.cs` + JS `applyTheme` | Carga y aplica el theme vía JS interop en `App.razor` (cubre login y app) |
| Blazor | `Components/Pages/Configuracion.razor` | Formulario admin: modo claro/oscuro, colores sidebar/header/footer, color primario |
| Blazor | `NavMenu.razor` | Enlace a Configuración visible solo para Admin |

### Claves de settings (básico)

| Clave | Valor por defecto | Uso |
|-------|-------------------|-----|
| `theme.colorMode` | `light` | `data-bs-theme` en `<html>` |
| `theme.sidebarBg` | `bg-body-secondary` | clase `bg-*` en `.app-sidebar` |
| `theme.headerBg` | `bg-body` | clase `bg-*` en `.app-header` |
| `theme.footerBg` | `bg-body` | clase `bg-*` en `.app-footer` |
| `theme.primary` | `#467FD0` | var CSS `--bs-primary` |

## 3. Seed de usuarios (admin + 1 por rol)

1. Añadir rol `Admin` a `IdentitySeeder` y **añadir claims de roles al JWT** en
   `AuthService.GenerateJwtToken` (hoy el token no incluye roles → `[Authorize(Roles=...)]` no funciona).
2. Extender el seed (ejecutado en `TaskFlow.Api/Program.cs` tras las migraciones):

| Email | Password | Roles |
|-------|----------|-------|
| `admin@taskflow.local` | `@Admin123/*-+` | Admin + Developer + TeamLead + QA (**acceso total**) |
| `developer@taskflow.local` | `@User123/*-+` | Developer |
| `teamlead@taskflow.local` | `@User123/*-+` | TeamLead |
| `qa@taskflow.local` | `@User123/*-+` | QA |

3. Se conserva el enlace `People.UserId ↔ AspNetUsers` por nombre/email para futuras importaciones
   (hoy la tabla `People` está vacía: 0 personas, 0 usuarios, 3 roles, 0 user-roles).

## 4. Verificación

- `dotnet build` de Blazor y API (0 errores).
- Aplicar migración `AddAppSettings` (auto al arrancar el API).
- Login como admin: cambio de tema visible en login y en la app.
- Login de usuarios por rol.
- `git push -u origin main` completado.

## Archivos relacionados

- `docs/Plan-Login-AdminLTE.md` (plan previo ya implementado)
- `D:\wamp64\www\AdminLTE\generate\theme.html` (referencia del theme)
- `TaskFlow.Infrastructure/Identity/IdentitySeeder.cs` (seed actual de roles)
- `TaskFlow.Infrastructure/Identity/AuthService.cs` (JWT sin claims de rol)