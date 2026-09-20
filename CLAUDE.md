# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Metodología de trabajo

- Este repositorio usa **BMAD** (Breakthrough Method for Agile AI Driven Development) para el flujo de desarrollo. Usar los agentes/personas de BMAD según corresponda: `bmad-agent-architect` (arquitectura), `bmad-agent-ux-designer` (UI/UX), `bmad-agent-analyst` (requisitos/dominio), `bmad-agent-dev` (implementación), QA e2e para verificación.
- El diseño de UI se basa en la plantilla **AdminLTE** local en `C:\wamp64\www\AdminLTE`, usando `starter.html` como página de referencia para estructura, componentes y estilos.

## Commands

There are no test projects in this solution. Build and run via the .NET CLI (net10.0, C#, `Nullable`/`ImplicitUsings` enabled in every project):

```bash
dotnet build TaskFlow.slnx                          # build everything
dotnet run --project TaskFlow.Api                    # API on http://localhost:5253 (https 7185)
dotnet run --project TaskFlow.Blazor                 # Blazor Server UI on http://localhost:5172 (https 7261)
```

Run both projects concurrently during development — the Blazor app is an interactive-server app that calls the API over HTTP, it is not a self-contained app.

EF Core migrations live in `TaskFlow.Infrastructure/Migrations`, generated/applied against `TaskFlow.Api` as the startup project:

```bash
dotnet ef migrations add <Name> -p TaskFlow.Infrastructure -s TaskFlow.Api
dotnet ef database update -p TaskFlow.Infrastructure -s TaskFlow.Api
```

Migrations are applied automatically on API startup (`dbContext.Database.MigrateAsync()` in `TaskFlow.Api/Program.cs`), which also runs `IdentitySeeder.SeedAsync` to create Identity roles/seed users.

## Architecture

Clean Architecture, five projects, dependencies flow inward:

```
TaskFlow.Api / TaskFlow.Blazor  →  TaskFlow.Infrastructure  →  TaskFlow.Application  →  TaskFlow.Domain
```

- **TaskFlow.Domain** — entities (`BaseEntity` with `CreatedAt`/`UpdatedAt`), enums, repository interfaces (`IUnitOfWork`, `I*Repository`), domain exceptions (`DomainException`, `NotFoundException`). No external dependencies.
- **TaskFlow.Application** — DTOs, service interfaces/implementations, FluentValidation validators, AutoMapper `MappingProfile`, and `Result<T>` (`Result<T>.Success/Failure` — services never throw for expected failure paths, controllers check `result.IsSuccess`).
- **TaskFlow.Infrastructure** — `TaskFlowDbContext` (extends `IdentityDbContext`, applies `IEntityTypeConfiguration<T>` from the assembly), EF repositories + `UnitOfWork`, ASP.NET Identity setup, JWT auth service, `IdentitySeeder`, and the TXT import parser (`Services/Txt/TxtTaskParser`). `DependencyInjection.AddInfrastructure` is the single place wiring DbContext, Identity, repositories, application services, AutoMapper and FluentValidation — register new repositories/services there.
- **TaskFlow.Api** — ASP.NET Core Web API, one controller per aggregate (`[Authorize]`, JWT bearer auth), `ExceptionHandlingMiddleware` maps `NotFoundException`→404 and `DomainException`→400 (unhandled → 500), Swagger with bearer auth configured.
- **TaskFlow.Blazor** — Blazor Server (interactive server render mode), talks to the API exclusively through `Services/ApiClient.cs` (typed `HttpClient`, one method per API endpoint). No direct DB/EF access from Blazor.

### Domain model (planning/import feature)

The core feature is importing developer task lists from monthly TXT files and managing them as structured planning data:

- `Period` (month/year) → `TaskGroup` (one block from the TXT: a `Project` + `Dev`/`TeamLead`/`QA` `Person` responsibles, or a QA-only block with `ProjectId`/`DevPersonId` null) → `PlanningTask` (parent task `Number` or subtask `Number.SubNumber`, `AssignedPersonId`, `Source` = `Import` or `Manual`).
- `ImportBatch` records each import run (file, encoding, counts, status) linked to a `Period`.
- Re-importing a file is idempotent by design (see `docs/Plan-Modelo-DB-TaskFlow.md`): a `TaskGroup` is matched by its unique key (period + project + dev, or period + QA for QA-only blocks); only `PlanningTask`s with `Source = Import` are replaced, manually-created tasks (`Source = Manual`) are preserved.
- `TxtTaskParser` must detect file encoding (BOM → UTF-8 → fallback Windows-1252) since source files come from Windows tools.
- Full parsing rules, table schema, and permission matrix are documented in `docs/Plan-Modelo-DB-TaskFlow.md` — read it before touching import/planning code.

### Auth

- JWT bearer auth end-to-end: `TaskFlow.Api` issues tokens (`AuthController`/`AuthService`), all other controllers are `[Authorize]`.
- Identity roles seeded by `IdentitySeeder`: `Admin`, `Developer`, `TeamLead`, `QA` (domain enum `Role` only has `Dev`/`TeamLead`/`Qa` — don't confuse the Identity role strings with the domain enum).
- `People.UserId` links a planning `Person` to an Identity `AspNetUsers.Id`.
- In Blazor, the JWT lives in the singleton `AuthTokenStore`; `AuthTokenHandler` (a `DelegatingHandler`) attaches it to every `ApiClient` request, and `AuthState` tracks the current session. Pages that require auth extend `AuthenticatedPageBase`, which redirects to `/login` on `OnAfterRenderAsync` when `AuthState.IsAuthenticated` is false.

### Frontend conventions (Blazor)

- Every page-level HTML form uses Bootstrap 5 "Custom Validation" (`class="needs-validation"`, `required` inputs, `invalid-feedback` divs) rather than manual `alert-danger` banners — see `docs/Plan-Validaciones-Toastr.md`. JS helper `window.taskflow.validateForm(form)` (in `wwwroot/js/validation.js`) validates and toggles `was-validated`; call it before invoking the API from a submit handler.
- User feedback (success/error/info/warning) goes through `ToastService` (`Services/ToastService.cs`, wraps `IJSRuntime`), which calls `window.taskflow.toast.*` in `wwwroot/js/notify.js`. That JS uses `toastr` if `window.toastr` is present, otherwise falls back to native Bootstrap 5 toasts — this machine has no internet access, so toastr/jQuery are not vendored and the Bootstrap fallback is what's actually active. Don't assume a CDN is reachable when touching this code.
- New pages needing auth + toast/nav should extend `AuthenticatedPageBase` rather than reimplementing the auth-redirect check.
