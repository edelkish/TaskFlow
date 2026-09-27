# Lógica de negocio — Importación de tareas TXT

Documento de referencia que explica cómo funciona el subsistema de planificación de TaskFlow:
qué valida la importación, qué escribe en la base, qué queda en el backlog y qué puede hacer
cada rol.

> **Marcadores de estado** usados en el texto:
> - ✅ comportamiento implementado y verificado
> - ⏳ pendiente de implementar
>
> Todos los pendientes que se habían detectado durante la revisión quedaron implementados el
> 2026-09-26. El detalle está en la [Parte 3](#parte-3--estado-de-los-pendientes).

---

## Índice

- [Parte 1 — La lógica de negocio](#parte-1--la-lógica-de-negocio)
- [Parte 2 — Ejemplo práctico con el archivo real](#parte-2--ejemplo-práctico-con-el-archivo-real)
- [Parte 3 — Estado de pendientes](#parte-3--lo-que-falta-para-que-esto-sea-la-realidad)

---

# Parte 1 — La lógica de negocio

## 1.1 La regla que gobierna todo

```
La importación NO crea maestros.  Todo o nada.  Un desconocido = archivo rechazado.
```

Todo lo demás se deriva de ahí. El importador es un **registrador**, no un creador: si el archivo dice
`Dev: Angel`, Angel tiene que existir en `Personas` con el cargo `Dev`. Si no, el archivo entero se
rechaza. ¿Por qué? Porque si el importador creara personas al vuelo, un typo en el TXT
(`Edelksi` en vez de `Edelkis`) metería un fantasma en el maestro y ese fantasma se propagaría a los
meses siguientes.

## 1.2 Tres conceptos que se confundían ✅

Este era el problema de fondo del diseño anterior:

| Concepto | Pregunta que responde | Dónde vive | Quién lo define |
|---|---|---|---|
| **Rol Identity** | *¿Qué puede hacer esta persona en la app?* | `AspNetRoles` | Tiende |
| **Cargo** (`Role`) | *¿Puede actuar como Dev / QA / TL en un TXT?* | tabla `Roles` | Curaduría |
| **Grupo de desarrollo** | *¿Quiénes trabajan juntos en este proyecto?* | `DevGroups` | Curaduría |

El `enum Role` de dominio se usaba para las dos primeras cosas. Ahora están separadas:

- `IdentitySeeder.cs:13-19` → roles de app: `Admin`, `Developer`, `TeamLead`, `QA`, `Planificación`
- `CatalogSeeder.cs` → cargos: `Dev`, `Team Lead`, `QA`, con GUIDs fijos (`111…`, `222…`, `333…`)
- `PersonRole` → puente N-a-N: **la misma persona puede tener varios cargos**, que es el caso de
  Edelkis en el archivo de ejemplo

**Por qué el backfill importa:** los cargos son nuevos. Si no se le asignaran a partir de los
`TaskGroups` históricos, **todos** los TXT que el equipo ya tiene se rechazarían. `CatalogSeeder`
recorre los bloques anteriores y le pone a cada persona los cargos que el archivo le exigía.

## 1.3 El grupo de desarrollo y por qué hoy solo avisa ✅

`DevGroup` es el equipo estable; `DevGroupMember` la membresía; `Project.DevGroupId` es **opcional**
(si borras el grupo, el proyecto queda sin grupo: `ON DELETE SET NULL`).

Sirve para **una sola cosa por ahora**: detectar cuando el Dev o el QA de un bloque no es del equipo
del proyecto.

Y es un **warning deliberado**, no un error:

```
Bloque 1: 'Angel' (Dev) no pertenece al grupo de desarrollo 'Core' del proyecto.
```

Razón: si fuera bloqueante, el día que hay que planificar el mes y el grupo todavía está
desactualizado, **no podrías reportar nada**. Un warning deja trabajar y dice qué limpiar. La idea es
promoverlo a error más adelante, cuando los datos estén depurados.

## 1.4 Período vs. Backlog: una sola tabla ✅

Antes había dos entidades para "tarea" y ninguna se usaba. Ahora hay una, y **la regla que las
distingue es `TaskGroupId`**:

| | Tarea de **período** | Tarea de **backlog** |
|---|---|---|
| Viene del TXT | Sí | No (se crea a mano) |
| `TaskGroupId` | apunta al bloque | `NULL` |
| `ProjectId` | el del bloque | el que elige el usuario |
| `Number` / `SubNumber` | `1`, `1.1` | `NULL` |
| `Source` | `Import` | `Manual` |
| Asignación | restringida al Dev/TL/QA **del grupo** | libre |
| Al borrar el grupo | se borran en cascada | no se ven afectadas |

Las tareas de período son **histórico de un mes**: describen qué se hizo en agosto. El backlog es
**trabajo futuro sin fecha**: vive en el proyecto, no en un mes.

> Por qué el KPI "Tareas planificadas" del dashboard antes siempre daba 0: leía `TaskItem`, una tabla
> huérfana que nadie llenaba. Ahora cuenta backlog real.

## 1.5 Permisos: reportar no es curar ✅

| Acción | Admin | Planificación | TeamLead | QA | Developer |
|---|:---:|:---:|:---:|:---:|:---:|
| Ver todo | ✅ | ✅ | ✅ | ✅ | ✅ |
| Maestros: Personas, Cargos, Proyectos, Períodos, Grupos, Grupos dev., Backlog | ✅ | ✅ | ❌ | ❌ | ❌ |
| Importar TXT | ✅ | ✅ | ✅ | ✅ | ❌ |

La idea: el Developer prepara su trabajo pero **no reporta** (para que no se altere lo que se le
asignó) y **no toca maestros**. Planificación cura maestros y también puede reportar, aunque no sea
Dev ni QA. TeamLead y QA reportan porque son responsables del trabajo del mes.

## 1.6 El pipeline, etapa por etapa

```
TXT ──► ① PARSER ──► ② VALIDADOR ──► ③ ESCRITURA ──► ④ LOTE
        lee         decide         transaccional    deja rastro
        tolera      bloquea        atómica
```

**① Parser** — permisivo a propósito. Nunca aborta, nunca descarta. Si una línea está rara, la anota
como *finding* y sigue, para reportar **todos** los problemas del archivo de una vez en vez de fallar
en el primero.

**② Validador** — no escribe nada. Decide.

| Bloquean (error) | Avisan (warning) |
|---|---|
| `PeriodNotFound` — el mes no está creado | `PersonNotInDevGroup` — Dev/QA fuera del equipo |
| `ProjectRequired` / `NotFound` / `Inactive` | `UnrecognizedLine` — línea rara, se importa igual |
| `PersonNotFound` / `PersonInactive` | `IgnoredLine` — texto suelto |
| `MissingRole` — la persona no tiene el cargo que el archivo le exige | |
| `BlockWithoutTasks` / `FileEmpty` | |

**③ Escritura** — una transacción. Si algo revienta a la mitad, `ROLLBACK` y la base queda como
estaba.

**④ Lote** — cada intento deja un `ImportBatch`. `PeriodId` es nullable a propósito: el rechazo más
común es *"el período no existe"*, y con la columna obligatoria ese rechazo no se podría ni
registrar.

---

# Parte 2 — Ejemplo práctico con el archivo real

`docs/samples/taskAgosto2026.txt`, línea por línea, con lo que el parser entiende:

```text
Agosto 2026                 → Período: mes=8, año=2026
App: Sitio Web (UI)         → Proyecto (debe existir ya)
Dev: Angel                  → Persona con cargo Dev
Team Leade: Edelkis         → Team Lead (el typo "Leade" está permitido)
QA: Yidsy                   → Persona con cargo QA
Tasks:                      → empieza la captura del bloque
1. Configurar entornos      → Number=1, SubNumber=NULL
   1.1- Crear pipelines...  → Number=1, SubNumber=1
       - nocturno y por rama→ se ANEXA a la subtarea 1.1 (no crea tarea)
   1.2- Parametrizar...     → Number=1, SubNumber=2
2. Maquetación de pantallas → Number=2
   2.1- Layout base AdminLTE→ Number=2, SubNumber=1
3. Accesibilidad            → Number=3
   3.1- Contraste AA...     → Number=3, SubNumber=1
```

Nótese tres cosas del parser que hacen la vida fácil:

- `Team Leade:` sin la `r` final → se acepta (y también `Team Lead:`)
- `1.1-` con guion o `1.1.` con punto → ambos
- El bullet `- nocturno y por rama` **se suma** a la descripción de 1.1, no crea una tarea suelta.
  Por eso el bloque 1 son 7 tareas y no 8.

## 2.1 Estado inicial necesario

```sql
-- Período: lo crea alguien en /periodos, no el importador
INSERT Periods (Name, Month, Year) VALUES ('Agosto 2026', 8, 2026);

-- Proyectos: deben existir con el nombre EXACTO del App:
INSERT Projects (Name, ...) VALUES ('Sitio Web (UI)', ...),
                                ('Sitio Web (API)', ...),
                                ('Portal de Reportes', ...);

-- Personas + cargos (la misma persona puede tener varios)
Angel      → Dev
Elizabeth   → Dev
Edelkis     → Dev + Team Lead      ← los dos, por eso la tabla puente N-a-N
Miguel      → Team Lead
Yidsy       → QA

-- Grupo de desarrollo (opcional)
DevGroup "Core" = { Angel, Elizabeth, Edelkis, Yidsy }
Project "Sitio Web (UI)" → DevGroupId = Core
```

## 2.2 El resultado: 4 bloques, 15 tareas

El parser agrupa así:

| # | `App:` | Dev | TL | QA | Tareas | `TaskGroup` que se crea |
|---|---|---|---|---|---|---|
| 1 | Sitio Web (UI) | Angel | Edelkis | Yidsy | 3 padres + 4 subs = **7** | período 8/2026, proyecto UI, dev=Angel |
| 2 | Sitio Web (API) | Elizabeth | Edelkis | Yidsy | 2 padres + 3 subs = **5** | período 8/2026, proyecto API, dev=Elizabeth |
| 3 | Portal de Reportes | Edelkis | Miguel | Yidsy | 1 padre + 1 sub = **2** | período 8/2026, proyecto Reportes, dev=Edelkis |
| 4 | *(no hay App:)* | — | Edelkis | Yidsy | **1** | **QA-only**: sin proyecto, sin dev |

Y las filas del bloque 1 en `PlanningTasks`:

| Number | Sub | Description | AssignedPerson | Project | Source |
|---|---|---|---|---|---|
| 1 | — | Configurar entornos | Angel | Sitio Web (UI) | Import |
| 1 | 1 | Crear pipelines de build · nocturno y por rama | Angel | Sitio Web (UI) | Import |
| 1 | 2 | Parametrizar connection strings | Angel | Sitio Web (UI) | Import |
| 2 | — | Maquetación de pantallas | Angel | Sitio Web (UI) | Import |
| 2 | 1 | Layout base AdminLTE | Angel | Sitio Web (UI) | Import |
| 3 | — | Accesibilidad | Angel | Sitio Web (UI) | Import |
| 3 | 1 | Contraste AA en formularios | Angel | Sitio Web (UI) | Import |

**La asignación es automática:** el importador no adivina. El archivo dice quién es el Dev del bloque,
y todo lo de ese bloque se le asigna a esa persona. En el bloque QA-only se asigna al QA.

## 2.3 Caso A — un Dev que no es del equipo del proyecto ✅

Si a Elizabeth la sacas del DevGroup "Core":

```json
{ "severity": "Warning", "code": "PersonNotInDevGroup", "line": 16, "field": "Dev",
  "message": "Bloque 2: 'Elizabeth' (Dev) no pertenece al grupo de desarrollo 'Core'.",
  "resolution": "Agregue a 'Elizabeth' al grupo 'Core' en Grupos de Desarrollo." }
```

`isValid: true` → **se importa igual**, y el warning queda en pantalla para que veas qué limpiar. El
mes se puede reportar.

## 2.4 Caso B — alguien que no existe → 409 y nada cambia

Si el archivo dice `Team Leade: Migel` (typo) y Miguel no está en `Personas`:

```
POST /api/import  →  409 Conflict

{
  "message": "La validación encontró 1 error. No se modificó nada.",
  "validation": {
    "isValid": false,
    "errorsCount": 1,
    "warningsCount": 0,
    "blocksCount": 4,
    "tasksCount": 15,
    "findings": [{
      "severity": "Error", "code": "PersonNotFound", "line": 27, "field": "Team Lead",
      "value": "Migel",
      "message": "Bloque 3: la persona 'Migel' (Team Lead) no existe.",
      "resolution": "Cree a 'Migel' en Personas antes de importar."
    }]
  }
}
```

Y lo importante: **no se creó nada**. Ni el período, ni los 3 bloques que estaban perfectos, ni las 14
tareas válidas. Todo o nada.

Además queda registrado el intento:

```sql
SELECT * FROM ImportBatches
-- Status: 'Failed', PeriodId: NULL, Note: 'PersonNotFound: Migel (Team Lead)',
-- TasksCount: 0   ← el registro de que alguien intentó esto y por qué falló
```

> **Nota de implementación.** Este registro de auditoría tuvo un defecto: el código asignaba
> `FilePath = null` al lote rechazado, pero la columna `ImportBatches.FilePath` es `NOT NULL`. El
> INSERT fallaba, el `catch` se lo comía en silencio y devolvía `null`, así que `rejectedBatchId`
> siempre salía `null` y **no quedaba rastro de ningún rechazo**. Ya está corregido: la ruta real
> se propaga en `ImportValidationDto` y el lote rechazado sí se escribe.

## 2.5 Caso C — reimportar el mismo archivo

La clave de cada bloque es **(período, proyecto, dev)** — o **(período, qa)** para el QA-only. Si ya
existe, se actualiza; y solo se borran las tareas con `Source = Import`:

```json
{ "periodName": "Agosto 2026",
  "groupsCreated": 0, "groupsUpdated": 4,
  "tasksImported": 15, "tasksRemoved": 15 }
```

**Las tareas manuales se conservan.** Si un Dev cargó a mano "Documentar API" dentro del bloque 2,
esa tarea sobrevive a la reimportación; las 5 importadas se reescriben.

## 2.6 Caso D — crear un backlog

Después, el backlog del proyecto, que **no tiene período ni número**:

```http
POST /api/planningtasks
{ "projectId": "<Sitio Web (API)>", "description": "Migrar a .NET 10",
  "assignedPersonId": "<Elizabeth>", "source": "Manual" }
→ 201 { "number": null, "subNumber": null, "isBacklog": true }
```

Y el filtro:

```
GET /api/planningtasks/backlog?projectId=<Sitio Web (API)>&assigneeId=<Elizabeth>
```

Sale en la pestaña **Backlog** de `/tareas`, y el KPI del dashboard ya cuenta.

## 2.7 Los endpoints

| Método | Ruta | Quién | Qué hace |
|---|---|---|---|
| POST | `/api/import/validate` | importador | Dry-run. **200 siempre** — el resultado *es* la respuesta |
| POST | `/api/import` | importador | Importa, o **409** con el detalle si hay errores |
| GET | `/api/import` | cualquiera | Historial de lotes |
| GET | `/api/planningtasks/backlog` | cualquiera | Backlog, filtros por proyecto/asignado |
| GET/POST/PUT/DELETE | `/api/planningtasks` | lectura todos · escritura Admin+Planif | Tareas de período y backlog |
| GET/POST/PUT/DELETE | `/api/devgroups` | lectura todos · escritura Admin+Planif | Grupos de desarrollo |

> Dry-run devuelve 200 incluso con errores a propósito: que el archivo tenga errores no es un fallo
> de la llamada, es el contenido de la respuesta. Por eso el `409` está solo en la importación real.

---

# Parte 3 — Estado de los pendientes

Los cinco puntos de esta lista estaban planteados cuando se escribió el documento. **Los cinco
quedaron implementados** el 2026-09-26.

| # | Cambio | Por qué | Estado |
|---|---|---|---|
| 1 | `PlanningTask.ProjectId` → nullable, y `ProjectRequired` solo cuando el bloque no es QA-only | El bloque 4 del archivo de ejemplo (QA-only, sin `App:`) se rechazaba, pero es un caso que el equipo usa y que el resto del sistema ya contemplaba | ✅ Hecho. Migración `MakePlanningTaskProjectOptional` (solo un `ALTER COLUMN` a nullable; los índices únicos no se tocan) |
| 2 | Propagar la ruta real al lote rechazado | `FilePath = null` contra una columna `NOT NULL` hacía fallar el INSERT; el `catch` lo silenciaba y los rechazos nunca se registraban | ✅ Hecho. `FilePath` agregado a `ImportValidationDto` |
| 3 | Clave de respaldo `(período, proyecto, qa)` para bloques con `App:` y sin `Dev:` | Ese tipo de bloque **se duplicaba en cada reimportación**: la búsqueda usaba la clave QA-only, que exige `ProjectId == NULL`, y el grupo sí tiene proyecto | ✅ Hecho. `GetByQaWithProjectBlockKeyAsync` |
| 4 | `Status = Partial` cuando hay warnings del **validador** | Se miraba `parsed.Findings` (del parser) en vez de los findings de la validación, así que un `PersonNotInDevGroup` dejaba el lote en `Success` con `WarningsCount = 0` | ✅ Hecho. Ahora cuenta parser + validador |
| 5 | `href="proyectos"` → `/dashboard`; gating visual en `/dashboard`, `/periodos`, `/grupos` | Enlace muerto; la API ya daba 403 pero los botones seguían visibles | ✅ Hecho con `AuthState.CanEditMasterData` |

## Lo que sigue pendiente

- **No hay proyecto de tests.** Toda la validación estricta está verificada solo a mano contra
  `docs/samples/taskAgosto2026.txt`. Es el punto donde un falso negativo importa de verdad.
- **Faltan datos de curación.** La base tiene 0 personas, 0 períodos y 0 task groups, así que
  cualquier TXT real se rechaza con `PersonNotFound` / `PeriodNotFound` hasta cargar las personas
  con sus cargos y crear los períodos. Es el orden correcto, pero hay que saberlo antes de probar.
- **Historial de lotes desalineado.** Al reimportar, `TaskGroup.ImportBatchId` se reasigna al lote
  nuevo, así que la asociación histórica del lote anterior queda desalineada. No afecta la
  idempotencia de los grupos, solo el reporte de qué lote creó cada grupo.
- **Paquetes.** `AutoMapper 12.0.1` con `NU1903` (vulnerabilidad de severidad alta) y
  `System.Text.Encoding.CodePages` con `NU1510` (innecesario para el TFM actual).

Ver también [`Plan-Validacion-Estricta-TXT.md`](Plan-Validacion-Estricta-TXT.md) §7, que tiene el
detalle de las cinco fases y el historial de desviaciones.
