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
/// </summary>
public class TxtTaskParser : ITaskFileParser
{
    private static readonly Regex PeriodRegex =
        new(@"^(?<month>[A-ZÁÉÍÓÚáéíóúÑñ]+\s*[A-ZÁÉÍÓÚáéíóúÑñ]*)\s+(?<year>\d{4})\s*$",
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
            result.Warnings.Add("El archivo está vacío.");
            return result;
        }

        var periodMatch = PeriodRegex.Match(lines[lineIndex]);
        if (!periodMatch.Success)
        {
            result.Warnings.Add($"No se pudo interpretar el periodo en la línea 1: '{lines[lineIndex]}'.");
            return result;
        }

        result.PeriodName = lines[lineIndex].Trim();
        var monthName = periodMatch.Groups["month"].Value.Trim();
        result.Year = int.Parse(periodMatch.Groups["year"].Value);
        result.Month = Months.TryGetValue(monthName, out var m) ? m : 0;
        if (result.Month == 0)
        {
            result.Warnings.Add($"Mes no reconocido: '{monthName}'.");
        }

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

            var line = raw.Trim();

            var appM = AppRegex.Match(raw);
            if (appM.Success)
            {
                AddGroupIfPending(result, current);
                current = new ParsedTaskGroupDto { ProjectName = appM.Groups["name"].Value.Trim() };
                lastTask = null;
                capturingTasks = false;
                continue;
            }

            var devM = DevRegex.Match(raw);
            if (devM.Success)
            {
                current ??= new ParsedTaskGroupDto();
                current.DevName = devM.Groups["name"].Value.Trim();
                lastTask = null;
                capturingTasks = false;
                continue;
            }

            var leadM = TeamLeadRegex.Match(raw);
            if (leadM.Success)
            {
                current ??= new ParsedTaskGroupDto();
                current.TeamLeadName = leadM.Groups["name"].Value.Trim();
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
                }
                else
                {
                    // Inicio de bloque QA-only (o cierre del bloque anterior).
                    AddGroupIfPending(result, current);
                    current = new ParsedTaskGroupDto { QaName = qaName };
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
                result.Warnings.Add($"Línea {i + 1} ignorada: '{line}'");
                continue;
            }

            var subM = SubTaskRegex.Match(raw);
            if (subM.Success)
            {
                lastTask = new ParsedTaskDto
                {
                    Number = int.Parse(subM.Groups["n"].Value),
                    SubNumber = int.Parse(subM.Groups["sub"].Value),
                    Description = subM.Groups["desc"].Value.Trim()
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
                    Description = topM.Groups["desc"].Value.Trim()
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

            result.Warnings.Add($"Línea {i + 1} no reconocida: '{line}'");
        }

        AddGroupIfPending(result, current);
        return result;
    }

    private static void AddGroupIfPending(ParsedTaskFileDto result, ParsedTaskGroupDto? group)
    {
        if (group == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(group.ProjectName)
            && string.IsNullOrWhiteSpace(group.DevName)
            && string.IsNullOrWhiteSpace(group.QaName))
        {
            result.Warnings.Add("Bloque incompleto descartado (sin App, Dev ni QA).");
            return;
        }

        if (string.IsNullOrWhiteSpace(group.TeamLeadName) && string.IsNullOrWhiteSpace(group.QaName))
        {
            result.Warnings.Add("Bloque descartado por falta de responsables (Team Lead / QA).");
            return;
        }

        result.Groups.Add(group);
    }

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