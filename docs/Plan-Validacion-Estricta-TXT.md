# Plan: Validación estricta del TXT de planificación

## 1. Contexto

Hoy la importación de tareas desde TXT es **permisiva**: el parser acepta cualquier nombre y
`ImportService` lo materializa. Eso produce tres fallos silenciosos:

1. **Un nombre desconocido de persona crea una persona nueva** (`GetOrCreatePersonAsync`,
   `ImportService.cs:176`). Un typo genera una `Person` duplicada e indistinguible de la real.
2. **Un `App:` desconocido no produce ningún warning y destruye datos.** El bloque cae a la clave
   del QA (`ImportService.cs:116-125`) y `DeleteByGroupWhereImportAsync` (línea 154) borra las
   tareas importadas de ese grupo.
3. **No hay preview.** `/import` es fire-and-forget (`Import.razor:167`) y hasta un archivo
   inválido muestra un toast verde, porque el toast solo lee `TasksImported`/`GroupsCreated`.

Además, los maestros que el import necesita no están curados: no existe catálogo de cargos
(`Person` no tiene rol; el enum `Role` es código muerto), no existe concepto de grupo de
desarrollo, y los períodos se crean como efecto secundario del import.

## 2. Decisiones tomadas

| Decisión | Elección | Motivo |
|---|---|---|
| Alcance del rechazo | **Atómico** — un nombre desconocido bloquea el archivo entero | Nunca deja datos a medias; el mensaje es inequívoco |
| Cargos en el TXT | **Solo 3 etiquetas** (`Dev`, `Team Leade`, `QA`) | El catálogo admite "otros" para asignación y permisos sin cambiar el formato de los archivos fuente reales |
| Modelo de cargos | **N-a-N** (`PersonRole`) | La misma persona es Dev y TeamLead en el mismo mes (Edelkis) |
| Curación de maestros | **Rol Identity nuevo `Planificación`** | Separa quien reporta de quien cura. `TeamLead`/`QA` importan; `Developer` no |
| Backlog | **Unificado en `PlanningTask`** (`TaskGroupId` nullable) | Elimina el concepto duplicado `TaskItem` y el KPI roto |

## 3. Modelo de dominio resultante

```
Role (catálogo por datos)  1 ──< PersonRole >── 1  Person
                                                     │
Project 1 ──< PlanningTask >── 1  TaskGroup >── 1  Period
   │                            (TaskGroupId NULL = backlog, sin período)
   └── DevGroup? ──< DevGroupMember >── 1  Person
```

`TaskGroup` conserva su significado documentado (`TaskGroup.cs:4`): **bloque mensual** de un TXT.
`PeriodId` e `ImportBatchId` siguen siendo required, y los índices únicos de idempotencia
(`TaskGroupConfiguration.cs:43-51`) no se tocan. El backlog son tareas con `TaskGroupId = NULL`,
colgadas de `Project` y no de un período.

## 4. Fases

### Fase 0 — Frenar pérdida de datos (hotfix)

| Qué | Archivo |
|---|---|
| Eliminar el fallback de proyecto desconocido a clave QA | `TaskFlow.Application/Services/ImportService.cs:116-125` |
| `TaskGroupService.CreateAsync` no asigna `ImportBatchId` → violación de FK | `TaskFlow.Application/Services/TaskGroupService.cs:55-63` |
| Toast verde cuando no se importó nada | `TaskFlow.Blazor/Components/Pages/Import.razor:169` |

### Fase 1 — Catálogo de cargos

Implementa la tabla diseñada en `Plan-Modelo-DB-TaskFlow.md` §3.4 pero nunca creada, y la hace
**por datos** para admitir cargos adicionales.

- `Roles`: `RoleId TINYINT IDENTITY PK`, `Name NVARCHAR(30) UNIQUE`, `IsActive`
- `PersonRole`: `PersonId` + `RoleId`, PK compuesta. N-a-N obligatorio
- Eliminar `TaskFlow.Domain/Enums/Role.cs` (código muerto) y definir el seed en una lista
- Mapeo explícito cargo→rol Identity (`Dev`→`Developer`, `TeamLead`→`TeamLead`, `QA`→`QA`) para
  no repetir la deriva actual
- Patrón a copiar: `PersonService` + `PeopleController` + validadores + `MappingProfile` + DI
- UI: página `/cargos` + checkboxes de cargos en `/personas`

### Fase 2 — Validación estricta del TXT

El parser sigue puro en Infrastructure, sin BD. La validación contra catálogos vive en
Application, respetando Clean Architecture.

- **Parser**: `LineNumber` en las etiquetas y en `ParsedTaskDto`, para que cada hallazgo apunte a su línea
- **`IImportValidator`** → `ImportValidationDto`, dry-run, **cero escrituras**

```csharp
public enum ImportFindingCode { PeriodNotFound, PersonNotFound, PersonMissingRole,
                                 ProjectNotFound, ProjectInactive, BlockWithoutTasks, ParserMalformedLine }
public record ImportFinding(ImportFindingCode Code, ImportFindingSeverity Severity,
                            string Message, int? Line, string? RawValue, string? BlockLabel);
public record ImportBlockValidationDto(string RawProject, string RawDev, string RawTeamLead, string RawQa,
                            bool IsQaOnly, Guid? ProjectId, Guid? DevId, Guid? TeamLeadId, Guid? QaId,
                            bool IsValid, IReadOnlyList<ImportFinding> Findings, int TaskCount);
public record ImportValidationDto(bool CanImport, string PeriodName, int? PeriodId,
                            IReadOnlyList<ImportBlockValidationDto> Blocks,
                            IReadOnlyList<ImportFinding> Findings, int TotalTasks);
```

Reglas bloqueantes:

1. El **período debe existir** pre-creado. Deja de crearse como efecto secundario del import.
2. `App:` debe existir en `Projects` y estar `IsActive`.
3. `Dev:` debe existir y tener el cargo `Dev`.
4. `Team Leade:` debe existir y tener el cargo `Team Lead`.
5. `QA:` debe existir y tener el cargo `QA`.
6. Cada bloque debe tener al menos una tarea.

Los warnings del parser pasan a Findings con severidad; los de nombre pasan a Blocking.

- `POST api/import/validate` (dry-run) y `POST api/import` → **409 + `ImportValidationDto`**
  si `CanImport == false`, sin escribir período, grupos, tareas ni personas
- **Auditoría**: en rechazo total se registra un `ImportBatch` con `Status = Failed` y los
  hallazgos en `Note`. La planificación no se toca, pero queda rastro del intento.
- UI `/import`: botón "Validar" → tabla por bloque (nombres crudos vs. resueltos) + lista de
  hallazgos con línea y severidad. "Importar" deshabilitado mientras haya blocking.

### Fase 3 — Rol `Planificación`

- `IdentitySeeder`: crear el rol + `planificacion@taskflow.local`
- Policies en `Program.cs`:
  - `MasterDataWrite` = {Admin, Planificación} → Personas, Proyectos, Períodos, Cargos, Grupos
  - `ImportWrite` = {Admin, Planificación, TeamLead, QA} → importación
  - `Developer` **no** importa
- Blazor: `CanWriteMasters` / `CanImport` para ocultar nav y acciones. Solo cosmético: la frontera
  real es el policy del API. Ojo: hoy `IsAdmin` se lee de `localStorage`.

### Fase 4 — Grupos de desarrollo

- `DevGroup` + `DevGroupMember` + `Project.DevGroupId?`, con CRUD y página
- Validación ampliada: el `Dev:`/`QA:` de un bloque con `App:` debe ser miembro del grupo del
  proyecto. **Primero Warning, luego Blocking** cuando los datos estén limpios
- Renombrar `Person.DevGroups`/`LeadGroups`/`QaGroups` (`Person.cs:14-16`): son props de
  navegación mal nombradas que apuntan a `TaskGroup`, no a grupos de desarrollo

### Fase 5 — Unificar backlog

| Cambio | Detalle |
|---|---|
| `PlanningTasks.TaskGroupId` | → nullable |
| `PlanningTasks.ProjectId` | → required en el backlog; nullable solo para el bloque QA-only (ver Desviación 5) |
| `PlanningTasks.Number` / `SubNumber` | → nullable: no aplican a una tarea de backlog |
| `Source` | sigue separando `Import` de `Manual` |
| `TaskItem` / `Tasks` / `TasksController` / `TaskService` | **Retirar**: sin UI, sin import, `AssignedToId` sin FK, y alimenta el KPI "Tareas planificadas" que hoy siempre da 0 |
| UI | `/tareas` gana vista Período / Backlog, filtrable por proyecto y persona |

## 5. Orden de ejecución

`Fase 0` → `backfill de cargos` → `Fase 1` → `Fase 3` → `Fase 2` → `Fase 4` → `Fase 5`

La Fase 3 va antes que la 2 a propósito: si el rol `Planificación` ya existe, la activación de la
validación estricta es un cambio de comportamiento aislado y no mezcla dos cosas nuevas.

## 6. Riesgos

1. **Backfill de cargos bloqueante.** Las personas creadas por imports previos están sin cargo.
   Activar la Fase 2 sin backfill hace fallar todo import futuro. Se deriva el cargo de en qué
   slot aparece cada persona en sus `TaskGroups` (`DevPersonId`/`TeamLeadPersonId`/`QaPersonId`).
2. **Collation inconsistente.** `Person.Name` es `Latin1_General_CI_AI` (insensible a acentos)
   pero el lookup usa `p.Name.ToLower() == name.ToLower()` (sensible). "José" vs "Jose" pasaría
   la validación y reventaría el índice único con un 500.
3. **Validador 200 vs columna `nvarchar(120)`** en `CreatePersonValidator` → mismo tipo de 500.
4. **Sin tests.** La validación estricta necesita un proyecto de tests sobre
   `docs/samples/taskAgosto2026.txt`.

---

## 7. Estado de implementación

Actualizado 2026-09-26. **Fases 0 a 5 cerradas.** Las cuatro migraciones están aplicadas en
`TaskFlowDb` y la solución compila sin errores.

Para la explicación de la lógica con ejemplos prácticos ver
[`Logica-de-Negocio-Importacion.md`](Logica-de-Negocio-Importacion.md).

### Migraciones aplicadas

| Migración | Contenido |
|---|---|
| `20260926000010_AddRolesAndPersonRoles` | Tablas `Roles` y `PersonRole`, seed de cargos |
| `20260926071328_MakeImportBatchPeriodOptional` | `ImportBatch.PeriodId` nullable para auditar rechazos |
| `20260926074141_AddDevGroupsAndUnifyBacklog` | `DevGroups`, `DevGroupMembers`, unificación del backlog, retiro de `Tasks` |
| `20260926142430_MakePlanningTaskProjectOptional` | `PlanningTasks.ProjectId` nullable (Desviación 5) |

### Cerrado

- **Fase 0** - `ImportService` ya no degrada un `App:` desconocido a la clave del QA. La última
  versión de `ImportService` eliminó por completo ese camino.
- **Fase 1** - `Role` (catálogo por datos, `nvarchar(30)`, seed por migración con GUIDs fijos) y
  `PersonRole` (PK compuesta). Enum `TaskFlow.Domain/Enums/Role.cs` eliminado. Migración
  `20260926000010_AddRolesAndPersonRoles`. `CatalogSeeder` hace el backfill de cargos derivándolos
  de los `TaskGroups` históricos. UI `/cargos` + checkboxes en `/personas`.
- **Fase 2** - `IImportValidator`/`ImportValidator` (cero escrituras) y `ImportService.ImportTaskFileAsync`
  reescrito: valida, audita el rechazo como `ImportBatch.Status = Failed`, y solo entonces importa
  dentro de una transacción. `POST api/import/validate` devuelve 200 con el detalle siempre;
  `POST api/import` devuelve 409 con `ImportRejectionDto` si hay un solo error. La UI obliga a
  validar antes de habilitar el botón Importar. Migración `MakeImportBatchPeriodOptional`.
- **Fase 3** - Rol Identity `Planificación` + usuario semilla. Policies `MasterDataWrite`
  (Admin + Planificación) e `ImportWrite` (Admin + Planificación + TeamLead + QA) aplicadas a
  escritura de maestros, `api/import` y `api/import/validate`. `AuthState.CanImport` /
  `CanEditMasterData` ocultan la navegación correspondiente.
- **Fase 4** - `DevGroup` + `DevGroupMember` (PK compuesta) + `Project.DevGroupId?` con
  `ON DELETE SET NULL`. CRUD en `api/devgroups` y página `/grupos-desarrollo` con gestión de
  miembros. Selector de grupo al crear y editar un proyecto. Finding `PersonNotInDevGroup` como
  **Warning** para `Dev` y `QA`, y solo si el proyecto tiene grupo asignado: si no, es un dato
  pendiente de curación y no un error del archivo. Las props mal nombradas de `Person` quedaron
  como `PeriodGroupsAsDev`/`PeriodGroupsAsTeamLead`/`PeriodGroupsAsQa`, con un comentario que las
  distingue de `DevGroup`.
- **Fase 5** - `PlanningTask` unificado: `TaskGroupId` nullable, `Number`/`SubNumber` nullable,
  e índices únicos filtrados por `TaskGroupId IS NOT NULL` (en SQL Server `NULL = NULL` dentro de
  un índice único, así que sin el filtro la segunda tarea de backlog violaría la unicidad).
  `TaskItem`, `Tasks`, `TaskService` y `TasksController` retirados. `api/planningtasks` con
  `api/planningtasks/backlog` y filtros por proyecto y asignado. `/tareas` con las pestañas
  Período y Backlog. `ProjectDto.TaskCount` ahora cuenta backlog real.

### Desviaciones respecto al plan original

1. **`TaskGroup.ImportBatchId` pasó a nullable.** El plan lo daba por required, pero
   `TaskGroupService.CreateAsync` no tenía de dónde sacar un `ImportBatchId` y por eso el
   grupo manual nunca se pudo persistir. Con la migración aplicada, el plan de la Fase 5
   (backlog con `TaskGroupId` nullable) se apoya en el mismo mecanismo.
2. **`ImportBatch.PeriodId` pasó a nullable.** Necesario para auditar el rechazo más común:
   un intento rechazado *por un periodo inexistente*. Con la columna required, ese rechazo no se
   podría registrar.
3. **El parser ya no descarta bloques.** Antes `AddGroupIfPending` los eliminaba en silencio; ahora
   los retiene y emite un finding. Es la diferencia entre "el archivo parece vacío" y "el bloque 4
   no tiene App, Dev ni QA".
4. **Nombres de códigos de finding más explícitos** que en el plan: `PersonMissingRole` se
   partió en `MissingRole` (cubre tanto "el cargo no existe" como "la persona no lo tiene") y
   `ParserMalformedLine` en `UnrecognizedLine`/`IgnoredLine`.
5. **`PlanningTasks.ProjectId` quedó nullable, no required.** El plan original de la Fase 5 lo daba
   por required, pero el bloque QA-only (solo `QA:` y `Team Leade:`, sin `App:`) es un caso real
   del formato que el equipo ya usa, y está en el archivo de ejemplo. Con `ProjectId` required,
   ese bloque se rechazaba con `ProjectRequired` y `docs/samples/taskAgosto2026.txt` no se podía
   importar. La regla que quedó: `App:` es obligatorio en todo bloque **salvo el QA-only**, y el
   backlog sigue exigiendo proyecto. Migración `MakePlanningTaskProjectOptional`.

### Defectos encontrados y corregidos al revisar el cierre

1. **La auditoría de rechazos nunca se escribía.** `RecordRejectionAsync` asignaba `FilePath = null`
   a un lote rechazado, pero `ImportBatches.FilePath` es `NOT NULL`. El INSERT fallaba, el `catch`
   lo silenciaba y `rejectedBatchId` siempre salía `null`: no quedaba rastro de ningún rechazo,
   que era justo el objetivo de la función. Se agregó `FilePath` a `ImportValidationDto` y se
   propagó la ruta real.
2. **Los bloques con `App:` y sin `Dev:` se duplicaban en cada reimportación.** Caían en
   `GetByQaBlockKeyAsync`, que exige `ProjectId IS NULL`, así que nunca encontraban el grupo
   existente. Se agregó `GetByQaWithProjectBlockKeyAsync` con la clave (período, proyecto, qa).
3. **El `Status` del lote ignoraba los warnings del validador.** Se calculaba con
   `parsed.Findings` (del parser) en vez de los findings de la validación, así que un
   `PersonNotInDevGroup` dejaba el lote en `Success` con `WarningsCount = 0`.
4. **Gating visual incompleto.** La API ya respondía 403, pero `/dashboard`, `/periodos` y
   `/grupos` seguían mostrando botones de escritura. Ahora usan `AuthState.CanEditMasterData`.
5. **Enlace roto** `href="proyectos"` en `/grupos-desarrollo`: la página de proyectos es
   `/dashboard`.

### Riesgos ya resueltos

- Riesgo 2 (collation): `PersonRepository.GetByNameCaseInsensitiveAsync` y
  `ProjectRepository.GetByNameCaseInsensitiveAsync` ahora comparan directo y dejan que la
  collation `Latin1_General_CI_AI` resuelva mayúsculas y acentos, coincidiendo con el índice
  único. Se añadió esa misma collation a `Projects.Name`.
- Riesgo 3 (200 vs 120): `CreatePersonValidator` y `UpdatePersonValidator` ahora usan 120.
- Riesgo 1 (backfill): resuelto por `CatalogSeeder`, que corre en el arranque de la API.

### Pendiente

- **Riesgo 4 (tests)**: sigue sin proyecto de tests. La validación estricta es el único punto
  donde un falso negativo importa de verdad, y ahora mismo solo está verificada a mano contra
  `docs/samples/taskAgosto2026.txt`.
- **Curaduría de datos**: la base tiene 0 `People`, 0 `TaskGroups` y 0 períodos, así que cualquier
  TXT real se rechaza con `PersonNotFound` / `PeriodNotFound` hasta que se carguen las personas
  con sus cargos y se creen los períodos. El orden es correcto, pero conviene saberlo antes de
  probar.
- **Historial de lotes**: al reimportar, `TaskGroup.ImportBatchId` se reasigna al lote nuevo, así
  que la asociación histórica del lote anterior queda desalineada. No afecta la idempotencia de
  los grupos, solo el reporte de qué lote creó cada grupo.
- **Paquetes**: `AutoMapper 12.0.1` arrastra `NU1903` (vulnerabilidad conocida de severidad alta) y
  `System.Text.Encoding.CodePages` arrastra `NU1510` (paquete innecesario para el TFM actual).
  Ninguno bloquea la compilación, pero son 10 de las advertencias del build.
