using System.Text;
using System.Text.RegularExpressions;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Infrastructure.Services.Txt;

/// <summary>
/// Parser de archivos TXT de planificación mensual de tareas.
/// Estructura esperada: periodo (mes + año), bloques "App/Dev/Team Leade/QA/Tasks:"
/// (tareas de desarrollador) o bloques "QA/Team Leade/Tasks:" (tareas globales de QA).
/// Las tareas empiezan con número; las subtareas usan patrón "N.M-"; las líneas
/// que empiezan con "-" continúan la descripción de la tarea anterior.
///
/// El parser es deliberadamente permisivo: no descarta bloques ni aborta ante líneas
/// raras, las registra como <see cref="ImportFindingDto"/> y sigue. Así la validación
/// puede reportar todos los problemas del archivo de una sola vez, en vez de fallar
/// en el primero. Quien decide si el archivo se importa es ImportValidator.
/// </summary>
public class TxtTaskParser : ITaskFileParser
{
    private static readonly Regex PeriodRegex =
        new(@"^(?<month>[A-Za-zÁÉÍÓÚáéíóúÑñ]+\s*[A-Za-zÁÉÍÓÚáéíóúÑñ]*)\s+(?<year>\d{4})\s*$",
            RegexOptions.Compiled);

    private static readonly Regex AppRegex = new(@"^\s*App\s*:\s*(?<name>.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex DevRegex = new(@"^\s*Dev\s*:\s*(?<name>.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex TeamLeadRegex = new(@"^\s*Team\s*Leade?\s*:\s*(?<name>.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex QaRegex = new(@"^\s*QA\s*:\s*(?<name>.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex TasksRegex = new(@"^\s*Tasks?\s*:\s*$", RegexOptions.Compiled);
    private static readonly Regex SubTaskRegex = new(@"^\s*(?<n>\d+)\.(?<sub>\d+)\s*[-\.]?\s*(?<desc>.*)$", RegexOptions.Compiled);
    private static readonly Regex TopTaskRegex = new(@"^\s*(?<n>\d+)\.\s+(?<desc>.*)$", RegexOptions.Compiled);
    private static readonly Regex BulletRegex = new(@"^\s*[-•]\s*(?<desc>.+)$", RegexOptions.Compiled);

    private static readonly Dictionary<string, int> Months = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enero"] = 1, ["febrero"] = 2, ["marzo"] = 3, ["abril"] = 4,
        ["mayo"] = 5, ["junio"] = 6, ["julio"] = 7, ["agosto"] = 8,
        ["septiembre"] = 9, ["setiembre"] = 9, ["octubre"] = 10,
        ["noviembre"] = 11, ["diciembre"] = 12
    };

    public async Task<ParsedTaskFileDto> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var text = await ReadAllTextWithEncodingAsync(filePath, cancellationToken);
        var encoding = DetectEncoding(filePath);
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        var result = new ParsedTaskFileDto
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            FileEncoding = encoding
        };

        int lineIndex = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                lineIndex = i;
                break;
            }
        }

        if (lineIndex < 0)
        {
            result.Findings.Add(Error(ImportFindingCode.FileEmpty, 0, 0, null, null,
                "El archivo está vacío.",
                "Agregue al menos la línea del periodo y un bloque de tareas."));
            return result;
        }

        var periodLine = lineIndex + 1;
        var periodMatch = PeriodRegex.Match(lines[lineIndex]);
        if (!periodMatch.Success)
        {
            result.Findings.Add(Error(ImportFindingCode.PeriodNotParsed, periodLine, 0, null, lines[lineIndex].Trim(),
                $"No se pudo interpretar el periodo: '{lines[lineIndex].Trim()}'.",
                "Use el formato 'Agosto 2026' (mes en letra seguido del año)."));
            return result;
        }

        result.PeriodName = lines[lineIndex].Trim();
        var monthName = periodMatch.Groups["month"].Value.Trim();
        result.Year = int.Parse(periodMatch.Groups["year"].Value);

        if (!Months.TryGetValue(monthName, out var month))
        {
            result.Findings.Add(Error(ImportFindingCode.MonthUnknown, periodLine, 0, null, monthName,
                $"Mes no reconocido: '{monthName}'.",
                "Use un mes en español, por ejemplo 'Agosto 2026'."));
            return result;
        }

        result.Month = month;

        ParsedTaskGroupDto? current = null;
        ParsedTaskDto? lastTask = null;
        bool capturingTasks = false;

        for (int i = lineIndex + 1; i < lines.Count; i++)
        {
            var raw = lines[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var lineNumber = i + 1;
            var line = raw.Trim();

            var appM = AppRegex.Match(raw);
            if (appM.Success)
            {
                CloseGroup(result, ref current);
                current = StartGroup(result, lineNumber);
                current.ProjectName = appM.Groups["name"].Value.Trim();
                current.ProjectLine = lineNumber;
                lastTask = null;
                capturingTasks = false;
                continue;
            }

            var devM = DevRegex.Match(raw);
            if (devM.Success)
            {
                if (current == null)
                {
                    current = StartGroup(result, lineNumber);
                }

                current.DevName = devM.Groups["name"].Value.Trim();
                current.DevLine = lineNumber;
                lastTask = null;
                capturingTasks = false;
                continue;
            }

            var leadM = TeamLeadRegex.Match(raw);
            if (leadM.Success)
            {
                if (current == null)
                {
                    current = StartGroup(result, lineNumber);
                }

                current.TeamLeadName = leadM.Groups["name"].Value.Trim();
                current.TeamLeadLine = lineNumber;
                lastTask = null;
                capturingTasks = false;
                continue;
            }

            var qaM = QaRegex.Match(raw);
            if (qaM.Success)
            {
                var qaName = qaM.Groups["name"].Value.Trim();

                if (current != null
                    && !string.IsNullOrWhiteSpace(current.DevName)
                    && string.IsNullOrWhiteSpace(current.QaName))
                {
                    // QA dentro de un bloque Dev.
                    current.QaName = qaName;
                    current.QaLine = lineNumber;
                }
                else
                {
                    // Inicio de bloque QA-only (o cierre del bloque anterior).
                    CloseGroup(result, ref current);
                    current = StartGroup(result, lineNumber);
                    current.QaName = qaName;
                    current.QaLine = lineNumber;
                    capturingTasks = false;
                }

                lastTask = null;
                continue;
            }

            if (TasksRegex.Match(raw).Success)
            {
                capturingTasks = true;
                lastTask = null;
                continue;
            }

            if (!capturingTasks || current == null)
            {
                result.Findings.Add(Warning(ImportFindingCode.IgnoredLine, lineNumber, current?.Index ?? 0,
                    null, line, $"Línea {lineNumber} ignorada: '{line}'",
                    "Borre la línea o muévala dentro de una sección 'Tasks:'."));
                continue;
            }

            var subM = SubTaskRegex.Match(raw);
            if (subM.Success)
            {
                lastTask = new ParsedTaskDto
                {
                    Number = int.Parse(subM.Groups["n"].Value),
                    SubNumber = int.Parse(subM.Groups["sub"].Value),
                    Description = subM.Groups["desc"].Value.Trim(),
                    Line = lineNumber
                };
                current.Tasks.Add(lastTask);
                continue;
            }

            var topM = TopTaskRegex.Match(raw);
            if (topM.Success)
            {
                lastTask = new ParsedTaskDto
                {
                    Number = int.Parse(topM.Groups["n"].Value),
                    SubNumber = null,
                    Description = topM.Groups["desc"].Value.Trim(),
                    Line = lineNumber
                };
                current.Tasks.Add(lastTask);
                continue;
            }

            var bulletM = BulletRegex.Match(raw);
            if (bulletM.Success && lastTask != null)
            {
                lastTask.Description += "\n" + bulletM.Groups["desc"].Value.Trim();
                continue;
            }

            result.Findings.Add(Warning(ImportFindingCode.UnrecognizedLine, lineNumber, current.Index,
                "Tasks", line,
                $"Línea {lineNumber} no reconocida: '{line}'",
                "Las tareas empiezan con '1.', '1.1-' o '-' para continuar la anterior."));
        }

        CloseGroup(result, ref current);
        return result;
    }

    private static ParsedTaskGroupDto StartGroup(ParsedTaskFileDto result, int lineNumber)
    {
        return new ParsedTaskGroupDto
        {
            Index = result.Groups.Count + 1,
            StartLine = lineNumber
        };
    }

    /// <summary>
    /// Cierra el bloque en curso y lo agrega al resultado. Antes el parser descartaba los
    /// bloques incompletos; ahora los conserva y los marca, porque en modo estricto un
    /// bloque sin responsables o sin tareas es un error que el usuario debe ver.
    /// </summary>
    private static void CloseGroup(ParsedTaskFileDto result, ref ParsedTaskGroupDto? group)
    {
        if (group == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(group.ProjectName)
            && string.IsNullOrWhiteSpace(group.DevName)
            && string.IsNullOrWhiteSpace(group.QaName))
        {
            result.Findings.Add(Error(ImportFindingCode.BlockWithoutOwner, group.StartLine, group.Index,
                null, null,
                $"Bloque {group.Index} descartado: no tiene App, Dev ni QA.",
                "Todo bloque debe indicar al menos un proyecto o un Dev, y un responsable."));
            group = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(group.TeamLeadName) && string.IsNullOrWhiteSpace(group.QaName))
        {
            result.Findings.Add(Error(ImportFindingCode.BlockWithoutOwner, group.StartLine, group.Index,
                null, null,
                $"Bloque {group.Index} descartado por falta de responsables (Team Lead / QA).",
                "Indique al menos un Team Lead o un QA en el bloque."));
            group = null;
            return;
        }

        if (group.Tasks.Count == 0)
        {
            result.Findings.Add(Error(ImportFindingCode.BlockWithoutTasks, group.StartLine, group.Index,
                "Tasks", null,
                $"El bloque {group.Index} no tiene tareas.",
                "Agregue al menos una tarea listada bajo 'Tasks:'."));
        }

        result.Groups.Add(group);
        group = null;
    }

    private static ImportFindingDto Error(ImportFindingCode code, int line, int blockIndex = 0,
        string? field = null, string? value = null, string message = "", string? resolution = null) =>
        new()
        {
            Code = code,
            Severity = ImportFindingSeverity.Error,
            Line = line,
            BlockIndex = blockIndex,
            Field = field,
            Value = value,
            Message = message,
            Resolution = resolution
        };

    private static ImportFindingDto Warning(ImportFindingCode code, int line, int blockIndex = 0,
        string? field = null, string? value = null, string message = "", string? resolution = null) =>
        new()
        {
            Code = code,
            Severity = ImportFindingSeverity.Warning,
            Line = line,
            BlockIndex = blockIndex,
            Field = field,
            Value = value,
            Message = message,
            Resolution = resolution
        };

    private static async Task<string> ReadAllTextWithEncodingAsync(string filePath, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        var encoding = DetectEncodingFromBytes(bytes);
        return encoding.GetString(bytes);
    }

    private static string DetectEncoding(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        return DetectEncodingFromBytes(bytes).WebName;
    }

    private static Encoding DetectEncodingFromBytes(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return new UTF8Encoding(true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode;
        }

        try
        {
            new UTF8Encoding(false, true).GetString(bytes);
            return new UTF8Encoding(false);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding("windows-1252", EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
        }
    }
}
