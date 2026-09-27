# Ejemplos de archivos TXT para importación

Esta carpeta contiene archivos de referencia con el formato que entiende
`TaskFlow.Infrastructure/Services/Txt/TxtTaskParser.cs`.

| Archivo | Contenido |
|---|---|
| `taskAgosto2026.txt` | Ejemplo completo y **sin advertencias**: 4 bloques Dev + 1 bloque QA-only, con subtareas, bullet de continuación, una App repetida entre dos Dev y una persona en dos roles. |

## Cómo importarlo

El import **no sube el archivo**: la API recibe la ruta absoluta **en el servidor**.

1. Copia `taskAgosto2026.txt` a una ruta accesible por la máquina donde corre la API
   (por ejemplo `C:\PT 2026\taskAgosto2026.txt`).
2. En TaskFlow, entra a `/import`, pega esa ruta y pulsa **Importar**.
3. El resultado muestra el periodo, los grupos creados/actualizados y las tareas importadas.

El endpoint equivalente es `POST api/import` con el cuerpo `{ "filePath": "C:\\PT 2026\\taskAgosto2026.txt" }`.

## Estructura esperada

El archivo es **orientado por líneas y basado en etiquetas**: no hay separador de columnas,
ni orden de columnas, ni fila de encabezado, ni comentarios.

```text
Agosto 2026                ← Periodo: {Mes} {Año}, primera línea no vacía
App: Sitio Web (UI)        ← Proyecto (debe existir ya en Projects)
Dev: Angel                 ← Persona con rol Dev
Team Leade: Edelkis        ← Persona con rol TeamLead (el typo "Leade" está permitido)
QA: Yidsy                  ← Persona con rol QA
Tasks:                     ← activa la captura de tareas del bloque
1. Tarea padre             ← Number = 1, SubNumber = NULL
   1.1- Subtarea           ← Number = 1, SubNumber = 1
       - bullet            ← se anexa a la descripción anterior
QA: Yidsy                  ← sin App:/Dev: ⇒ nuevo bloque QA-only
Team Leade: Edelkis
Tasks:
1. Pruebas globales del mes
```

| Elemento | Regla (regex) |
|---|---|
| Periodo | `^(?<month>...)\s+(?<year>\d{4})\s*$` — sin sangría, al inicio |
| `App:` | `^\s*App\s*:\s*(?<name>.+?)\s*$` |
| `Dev:` | `^\s*Dev\s*:\s*(?<name>.+?)\s*$` |
| `Team Leade:` | `^\s*Team\s*Leade?\s*:\s*(?<name>.+?)\s*$` — acepta `Team Lead:` y `Team Leade:` |
| `QA:` | `^\s*QA\s*:\s*(?<name>.+?)\s*$` |
| `Tasks:` | `^\s*Tasks?\s*:\s*$` — nada después de los `:` |
| Tarea padre | `^\s*(?<n>\d+)\.\s+(?<desc>.*)$` — exige un espacio tras el punto |
| Subtarea | `^\s*(?<n>\d+)\.(?<sub>\d+)\s*[-\.]?\s*(?<desc>.*)$` — el `-` o `.` es opcional |
| Bullet | `^\s*[-•]\s*(?<desc>.+)$` — solo si ya hubo una tarea o subtarea |

### Meses aceptados

`enero`, `febrero`, `marzo`, `abril`, `mayo`, `junio`, `julio`, `agosto`, `septiembre`
(alias `setiembre`), `octubre`, `noviembre`, `diciembre`. Case-insensitive.
Cualquier otro nombre de mes produce el warning `Mes no reconocido` y **no se persiste nada**.

### Reglas de bloque

- Un bloque necesita **al menos** `Team Leade:` o `QA:`; si faltan ambos, se descarta
  (excepto los bloques Dev, que sí tienen `QA:`).
- Un bloque sin `App:`, sin `Dev:` y sin `QA:` se descarta.
- `App:` es **obligatorio en todo bloque salvo el QA-only**. Un bloque con `Dev:` y sin `App:`
  se rechaza con el error bloqueante `ProjectRequired`.
- El bloque QA-only (sin `App:` ni `Dev:`) es un caso válido del formato: sus tareas cuelgan
  del período y quedan **sin proyecto** (`TaskGroup.ProjectId` y `PlanningTask.ProjectId` en NULL).
- Las tareas de un bloque Dev se asignan al **Dev**; las del bloque QA-only, al **QA**.

## Puntos que suelen morder

- **El proyecto debe existir.** `App:` no crea proyectos: si el nombre no coincide
  (case-insensitive) con ninguno en `Projects`, el bloque se rechaza con `ProjectNotFound`.
  Crea los proyectos primero desde el CRUD.
- **Cualquier warning cambia el estado del lote a `Partial`** (aunque las tareas sí se
  importen). Cuenta tanto los warnings del parser (líneas raras) como los del validador
  (`PersonNotInDevGroup`). Un archivo limpio termina en `Success`.
- **Encoding**: se detecta en este orden → BOM UTF-8, UTF-16 LE, UTF-16 BE, UTF-8 estricto,
  y por último `windows-1252`. Los archivos que exportan las herramientas de Windows
  suelen ser cp1252, por eso el fallback.
- **Fin de línea**: LF y CRLF funcionan. Las líneas en blanco se ignoran.
- **Re-importar es idempotente**: el `TaskGroup` se localiza por su clave única
  (período + proyecto + dev; período + proyecto + qa si el bloque no trae Dev; o
  período + qa para el QA-only) y solo se reemplazan las tareas con `Source = Import`;
  las manuales (`Source = Manual`) se conservan.
- El parser **no soporta comentarios**: una línea `# ...` o `// ...` se cuenta como
  warning y baja el lote a `Partial`.

## Errores frecuentes

| Síntoma | Causa |
|---|---|
| `No se pudo interpretar el periodo en la línea 1` | Primera línea no vacía con formato `{Mes} {Año}`, o con sangría |
| `Mes no reconocido: '...'` y cero tareas | Nombre de mes fuera de la lista |
| `Línea N ignorada: '...'` | Texto suelto antes del `Tasks:` o sin bloque activo |
| `Línea N no reconocida: '...'` | Línea con formato inválido dentro de la zona de tareas |
| `Bloque descartado por falta de responsables` | Bloque sin `Team Leade:` ni `QA:` |
| `Falta la línea 'App:' con el proyecto` | Bloque con `Dev:` y sin `App:` (solo el QA-only puede omitirlo) |
| `El proyecto '...' no existe` | El nombre de `App:` no coincide con ningún proyecto de `Projects` |
