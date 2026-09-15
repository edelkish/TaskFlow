# Plan: Modelo de Base de Datos SQL Server para importación de tareas desde TXT

## 1. Contexto y objetivo

TaskFlow (Blazor WebAssembly .NET 10 + API .NET 10) gestiona tareas asociadas a proyectos para grupos de desarrollo (desarrolladores, jefes de proyecto y QA). Las tareas se cargan siempre desde un archivo TXT (ej. `D:\PT 2026\taskAgosto2026.txt`), y además se podrán administrar manualmente con CRUD.

Este plan define el modelo de datos **SQL Server 2022** en el servidor `ATISSGG07\SQLServer2022`, base de datos `TaskFlowDb`, bajo **Arquitectura Limpia** y principios **SOLID**, con la plantilla de UI **AdminLTE** (`D:\wamp64\www\AdminLTE`, página base `starter.html`) y la metodología **BMAD**.

## 2. Análisis de la estructura TXT

Formato observado en `D:\PT 2026\taskAgosto2026.txt`:

```
Agosto 2026                     ← Periodo (Mes + Año), primera línea
App: Aplicación ...             ← Proyecto
Dev: Elizabeth                  ← Persona (rol Dev)
Team Leade: Edelkis             ← Persona (rol TeamLead) — typo "Leade" en el archivo
QA: Yidsy                       ← Persona (rol QA)
Tasks:
1. <descripción>                ← Tarea padre (Number=1, SubNumber=NULL)
   1.1- <descripción>           ← Subtarea (Number=1, SubNumber=1)
       - <texto>                ← Bullet de continuación (pertenece a la descripción anterior)
22. <descripción>
```

### Reglas detectadas

- **Periodo**: primera línea `{Mes} {Año}`. El archivo es mensual.
- **Bloque Dev**: sección `App: / Dev: / Team Leade: / QA: / Tasks:`. Las tareas pertenecen al **Dev** de ese bloque.
- **Bloque QA-only**: sección que empieza por `QA: / Team Leade: / Tasks:` (sin `App:` ni `Dev:`). Las tareas pertenecen al **QA**, son globales del mes. `ProjectId`/`DevPersonId` = NULL.
- **Jerarquía de tareas**: las tareas empiezan siempre con número (`N.`). Las subtareas llevan subnúmero (`N.M-`). Los bullets `-` sin número continúan la descripción de la tarea o subtarea previa.
- **Personas repetidas**: la misma persona puede tener varios roles (Edelkis es Dev y TeamLead) → normalización en tabla `People`.
- **Misma App repetida**: `Sitio Web ... (UI)` aparece con Dev Angel y con Dev Edelkis → un `Project` reutilizado, dos `TaskGroups`.
- **Encoding**: el archivo parece Windows-1252 (mojibake `�` al leer como UTF-8) → el parser debe detectar la codificación (BOM → UTF-8 estricta → fallback cp1252).

## 3. Modelo de Base de Datos (SQL Server 2022 — `TaskFlowDb`)

### 3.1 `Periods`

| Columna | Tipo | Restricciones |
|---|---|---|
| PeriodId | INT IDENTITY | PK |
| Year | SMALLINT | NOT NULL |
| Month | TINYINT | NOT NULL |
| Name | NVARCHAR(60) | NOT NULL, ej. "Agosto 2026" |
| CreatedAt | DATETIME2 | NOT NULL, default `SYSUTCDATETIME()` |

Índice: `UQ (Year, Month)`.

### 3.2 `Projects`

| Columna | Tipo | Restricciones |
|---|---|---|
| ProjectId | INT IDENTITY | PK |
| Name | NVARCHAR(200) | NOT NULL, único |
| IsActive | BIT | NOT NULL, default `1` |
| CreatedAt | DATETIME2 | NOT NULL |

Índice: `UQ (Name)`.

### 3.3 `People`

| Columna | Tipo | Restricciones |
|---|---|---|
| PersonId | INT IDENTITY | PK |
| Name | NVARCHAR(120) | NOT NULL, único |
| UserId | NVARCHAR(450) | NULL, FK → AspNetUsers.Id (vínculo usuario logueado ↔ persona) |
| IsActive | BIT | NOT NULL, default `1` |
| CreatedAt | DATETIME2 | NOT NULL |

Índice: `UQ (Name)` (collation case-insensitive).

### 3.4 `Roles` (catálogo)

| Columna | Tipo | Restricciones |
|---|---|---|
| RoleId | TINYINT | PK (1=Dev, 2=TeamLead, 3=QA) |
| Name | NVARCHAR(30) | NOT NULL |

### 3.5 `ImportBatches`

| Columna | Tipo | Restricciones |
|---|---|---|
| ImportBatchId | INT IDENTITY | PK |
| PeriodId | INT | FK → Periods, NOT NULL |
| FileName | NVARCHAR(260) | NOT NULL |
| FilePath | NVARCHAR(500) | NOT NULL |
| FileEncoding | NVARCHAR(20) | NOT NULL (utf-8 / windows-1252) |
| Status | NVARCHAR(20) | NOT NULL (Success / Partial / Failed) |
| SectionsCount | INT | NOT NULL default 0 |
| TasksCount | INT | NOT NULL default 0 |
| WarningsCount | INT | NOT NULL default 0 |
| Note | NVARCHAR(MAX) | NULL (warnings/errores) |
| ImportedAt | DATETIME2 | NOT NULL |

### 3.6 `TaskGroups` (bloque del TXT)

| Columna | Tipo | Restricciones |
|---|---|---|
| TaskGroupId | INT IDENTITY | PK |
| ImportBatchId | INT | FK → ImportBatches, NOT NULL |
| PeriodId | INT | FK → Periods, NOT NULL |
| ProjectId | INT | FK → Projects, NULL (en bloques QA-only) |
| DevPersonId | INT | FK → People, NULL (en bloques QA-only) |
| TeamLeadPersonId | INT | FK → People, NULL |
| QaPersonId | INT | FK → People, NULL |
| CreatedAt | DATETIME2 | NOT NULL |
| UpdatedAt | DATETIME2 | NULL |

Índices únicos filtrados (idempotencia):
- `UX (PeriodId, ProjectId, DevPersonId) WHERE ProjectId IS NOT NULL AND DevPersonId IS NOT NULL`
- `UX (PeriodId, QaPersonId) WHERE ProjectId IS NULL AND DevPersonId IS NULL AND QaPersonId IS NOT NULL`

### 3.7 `PlanningTasks`

| Columna | Tipo | Restricciones |
|---|---|---|
| PlanningTaskId | INT IDENTITY | PK |
| TaskGroupId | INT | FK → TaskGroups (ON DELETE CASCADE), NOT NULL |
| Number | INT | NOT NULL (nº tarea padre, 1..22) |
| SubNumber | INT | NULL (NULL = tarea padre; `N` si es `N.M`) |
| AssignedPersonId | INT | NULL, FK → People (asignación libre) |
| Source | TINYINT | NOT NULL (0=Import, 1=Manual) |
| Description | NVARCHAR(MAX) | NOT NULL (incluye bullets de continuación) |
| CreatedAt | DATETIME2 | NOT NULL |
| UpdatedAt | DATETIME2 | NULL |

Índices únicos filtrados:
- `UX (TaskGroupId, Number) WHERE SubNumber IS NULL`
- `UX (TaskGroupId, Number, SubNumber) WHERE SubNumber IS NOT NULL`

Orden de lectura: `ORDER BY Number, SubNumber` (NULL primero vía `COALESCE`).

### 3.8 Identity (existente, se amplía)

- Roles seed: `Developer`, `TeamLead`, `QA` (mapean al catálogo `Roles`).
- FK `People.UserId → AspNetUsers.Id`.

## 4. Flujo de importación

### 4.1 Parser (`Infrastructure/Services/TxtTaskParser`)

1. **Detección de encoding**: BOM → UTF-8 estricta → fallback `Windows-1252` (registrar `CodePagesEncodingProvider` en `Program.cs`; paquete `System.Text.Encoding.CodePages`).
2. **Orientación por líneas** (trim):
   - Header periodo `{Mes} {Año}` (primera línea no vacía) → resolver/crear `Period`.
   - `App: <nombre>` → nuevo bloque Dev (finalizar bloque anterior).
   - `Dev: <nombre>` / `Team Leade: <nombre>` / `QA: <nombre>` → resolver persona.
   - `QA:` sin `App:` activo → nuevo **bloque QA-only**.
   - `Tasks:` → activar captura de tareas.
   - `^(\d+)\.\s+` → tarea padre `(Number, NULL)`.
   - `^[\t ]*(\d+)\.(\d+)[-\s.]+` → subtarea `(Number, SubNumber)`.
   - `^[\t ]*-` → bullet de continuación: anexar a `Description` del último task con `\n`.
   - Línea desconocida → warning (contador).
3. **Resolución de maestros**: `People`, `Projects`, `Period` por nombre (case-insensitive, lookup → create).
4. **Persistencia idempotente (Reemplazar)**: buscar `TaskGroup` por clave única filtrada. Si existe → actualizar responsables + eliminar solo `PlanningTasks` con `Source = Import` (conserva manuales) e insertar los nuevos; nuevo `ImportBatch` con historial. Si no existe → insertar.

### 4.2 Servicios y endpoints

- `IImportService.ImportTaskFileAsync(string filePath)` → `Result<ImportResultDto>` (batch id, grupos creados/actualizados, tareas, warnings).
- `GET api/imports/batches` / `GET api/imports/groups` / `GET api/imports/tasks` → consulta histórica.
- `POST api/imports` → iniciar importación indicando ruta de archivo (o subida multipart).

## 5. CRUD manual

- `TaskGroupsController`: alta/baja/modificación/consulta de grupos (cambiar Proyecto, Periodo, responsables).
- `PlanningTasksController`: alta/baja/edición/reasignación de tareas (`AssignedPersonId` validado ∈ miembros del grupo; `Source = 1 (Manual)` al crear desde UI).
- `PeriodsController` / `ProjectsController` / `PeopleController`: mantenimiento de maestros.
- Regla: la re-importación no borra tareas manuales.

## 6. Autenticación y autorización (vista de planificación)

- JWT existente (AuthService/Identity) + roles Identity: `Developer`, `TeamLead`, `QA`.
- Vinculación `People.UserId ↔ AspNetUsers.Id`.
- Matriz de permisos:
  - **TeamLead**: lectura/escritura total.
  - **Developer**: lectura total; escritura sobre sus bloques (DevPersonId = yo) y sus tareas asignadas.
  - **QA**: lectura total; escritura sobre bloques QA-only y tareas QA.
- Controllers con `[Authorize(Roles=...)]` + checks de propiedad; UI Blazor oculta acciones según rol (AdminLTE).

## 7. Mapeo a Clean Architecture (TaskFlow)

| Capa | Artefactos |
|---|---|
| **Domain** | `Period`, `Person` (+UserId), `Role` (enum), `TaskGroup`, `PlanningTask` (+AssignedPersonId, +Source), `ImportBatch`; enums `Role`, `ImportStatus`, `TaskSource`. `Project` ya existe → relación con `TaskGroup`. |
| **Application** | `IImportService`/`ImportService`, `ITaskGroupService`/`TaskGroupService`, `IPlanningTaskService`/`PlanningTaskService`, servicios CRUD de maestros; DTOs (`ImportBatchDto`, `ImportResultDto`, `TaskGroupDto`, `PlanningTaskDto`, `PeriodDto`, `PersonDto`, `ProjectDto`); validators FluentValidation (grupo, tarea, reasignación); profiles AutoMapper. |
| **Infrastructure** | `TxtTaskParser`, resolución de encoding; DbSets nuevos + `EntityTypeConfigurations` (`.HasFilter()` para índices filtrados); repos `PeriodRepository`, `PersonRepository`, `ProjectRepository`, `TaskGroupRepository`, `PlanningTaskRepository`, `ImportBatchRepository`; seed de roles Identity; registros en DI y `UnitOfWork`. |
| **API** | `ImportController`, `TaskGroupsController`, `PlanningTasksController`, `PeriodsController`, `ProjectsController`, `PeopleController`. |
| **Blazor** | Páginas de planificación (lista por periodo/grupo/persona), CRUD con modales, selector de asignación, filtros por rol, plantilla AdminLTE (`D:\wamp64\www\AdminLTE`, `starter.html`). |

## 8. Migración y despliegue

1. **Conexión** en `appsettings.json` (o user-secrets):
   `Server=ATISSGG07\SQLServer2022;Database=TaskFlowDb;User ID=sa;Password=sq;TrustServerCertificate=True;Encrypt=True`
   ⚠️ No exponer credenciales `sa` en repositorios (usar user-secrets / variables de entorno).
2. **Migraciones**: reemplazar `EnsureCreatedAsync()` por `database.MigrateAsync()`.
3. **Generar**: `dotnet ef migrations add InitialTaskFlow -p TaskFlow.Infrastructure -s TaskFlow.Api`
4. **Aplicar**: `dotnet ef database update -p TaskFlow.Infrastructure -s TaskFlow.Api` (crea la BD si no existe; `sa` tiene permiso).
5. **Validación E2E**: importar `D:\PT 2026\taskAgosto2026.txt` y comprobar: 4 bloques Dev + 1 bloque QA, jerarquías de tareas (ej. tarea 1 con subtareas en bloques UI/API), personas únicas (Edelkis 1 persona, 2+ grupos), re-importación idempotente, CRUD y auth por rol.

## 9. Supuestos/confirmaciones

- BD: `TaskFlowDb`. Re-importación: **Reemplazar** (solo `Source=Import`) con historial en `ImportBatches`. Asignación: automática al importar (Dev o QA) y **libre** en edición.
- Aceptar typo `Team Leade`.
- El bloque QA-only no se asocia a proyecto (`ProjectId` NULL).
- Apps `(UI)`/`(API)` se tratan como proyectos distintos (tal como aparecen en el archivo).
- Roles Identity `Developer`/`TeamLead`/`QA` seed inicial.

## 10. Notas de seguridad y pendientes del repo actual

- La cadena de conexión actual (`appsettings.json`) usa `localhost`/`TaskFlowDb` y `Trusted_Connection`; se actualizará a la instancia `ATISSGG07\SQLServer2022`.
- `NuGet.config` contiene credenciales de proxy en texto plano: revisar/mover a user-secrets (pendiente de desbloqueo).
- Puertos inconsistentes detectados: API 5253/7185 vs `ApiClient`→5002 vs CORS 5000/5001. Ajustar durante implementación del frontend.