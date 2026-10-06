using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

/// <summary>
/// Valida un TXT contra los maestros ya cargados. Regla central: la importación es
/// todo-o-nada, así que un solo desconocido (periodo, proyecto, Dev, Team Lead o QA)
/// bloquea el archivo completo. El validador no crea ni modifica nada.
/// </summary>
public class ImportValidator : IImportValidator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskFileParser _parser;

    public ImportValidator(IUnitOfWork unitOfWork, ITaskFileParser parser)
    {
        _unitOfWork = unitOfWork;
        _parser = parser;
    }

    public async Task<ImportValidationDto> ValidateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var parsed = await _parser.ParseAsync(filePath, cancellationToken);

        var findings = new List<ImportFindingDto>(parsed.Findings);

        var period = parsed.Month > 0 && parsed.Year > 0
            ? await _unitOfWork.Periods.GetByMonthYearAsync(parsed.Month, parsed.Year)
            : null;

        if (parsed.Month > 0 && parsed.Year > 0 && period == null)
        {
            findings.Add(Error(
                ImportFindingCode.PeriodNotFound, 1, 0, null, parsed.PeriodName,
                $"El periodo '{parsed.PeriodName}' no está creado.",
                "Cree el periodo en Periodos antes de importar; la importación no crea maestros."));
        }

        // Los cargos se resuelven una vez y se reutilizan en todos los bloques.
        var cargos = await LoadRequiredRolesAsync(cancellationToken);
        var blocks = new List<ImportBlockPreviewDto>();

        // El nombre de una persona ya no es único: la clave real es (Apellidos, Nombre).
        // Por eso la caché guarda la lista de coincidencias y no una persona, para poder
        // distinguir "no existe" de "existen varias" y reportar la ambigüedad en vez de
        // elegir una al azar y asignarle el bloque.
        var personCache = new Dictionary<string, List<Person>>(StringComparer.OrdinalIgnoreCase);
        var projectCache = new Dictionary<string, Project?>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in parsed.Groups)
        {
            blocks.Add(await ValidateBlockAsync(group, cargos, personCache, projectCache, cancellationToken));
        }

        var errorsCount = findings.Count(f => f.Severity == ImportFindingSeverity.Error);
        var warningsCount = findings.Count - errorsCount;

        return new ImportValidationDto
        {
            IsValid = errorsCount == 0,
            FileName = parsed.FileName,
            FilePath = parsed.FilePath,
            FileEncoding = parsed.FileEncoding,
            PeriodName = parsed.PeriodName,
            Month = parsed.Month > 0 ? parsed.Month : null,
            Year = parsed.Year > 0 ? parsed.Year : null,
            PeriodId = period?.Id,
            BlocksCount = parsed.Groups.Count,
            TasksCount = parsed.Groups.Sum(g => g.Tasks.Count),
            ErrorsCount = errorsCount,
            WarningsCount = warningsCount,
            Findings = findings.OrderBy(f => f.Line).ThenBy(f => f.BlockIndex).ToList(),
            Blocks = blocks
        };
    }

    private async Task<ImportBlockPreviewDto> ValidateBlockAsync(
        ParsedTaskGroupDto group,
        IReadOnlyDictionary<string, Role?> cargos,
        Dictionary<string, List<Person>> personCache,
        Dictionary<string, Project?> projectCache,
        CancellationToken cancellationToken)
    {
        var blockFindings = new List<ImportFindingDto>();

        Project? project = null;

        // El bloque QA-only no declara App: y es un caso válido del formato, por eso se
        // salta la exigencia. Cualquier otro bloque sí necesita proyecto.
        if (!group.IsQaOnly && string.IsNullOrWhiteSpace(group.ProjectName))
        {
            blockFindings.Add(Error(
                ImportFindingCode.ProjectRequired, group.StartLine, group.Index, "App", null,
                $"Bloque {group.Index}: falta la línea 'App:' con el proyecto.",
                "Cada bloque debe declarar su proyecto; la tarea cuelga del proyecto."));
        }
        else
        {
            var name = group.ProjectName.Trim();
            if (!projectCache.TryGetValue(name, out project))
            {
                project = await _unitOfWork.Projects.GetByNameCaseInsensitiveAsync(name);
                projectCache[name] = project;
            }

            if (project == null)
            {
                blockFindings.Add(Error(
                    ImportFindingCode.ProjectNotFound, group.ProjectLine, group.Index, "App", name,
                    $"Bloque {group.Index}: el proyecto '{name}' no existe.",
                    "Cree el proyecto en Proyectos antes de importar."));
            }
            else if (!project.IsActive)
            {
                blockFindings.Add(Error(
                    ImportFindingCode.ProjectInactive, group.ProjectLine, group.Index, "App", name,
                    $"Bloque {group.Index}: el proyecto '{name}' está inactivo.",
                    "Reactive el proyecto antes de importar."));
            }
        }

        if (!string.IsNullOrWhiteSpace(group.DevName))
        {
            var finding = await ValidatePersonAsync(group, "Dev", group.DevName, group.DevLine,
                cargos, personCache, cancellationToken);
            if (finding != null) blockFindings.Add(finding);
        }

        if (!string.IsNullOrWhiteSpace(group.TeamLeadName))
        {
            var finding = await ValidatePersonAsync(group, "Team Lead", group.TeamLeadName, group.TeamLeadLine,
                cargos, personCache, cancellationToken);
            if (finding != null) blockFindings.Add(finding);
        }

        if (!string.IsNullOrWhiteSpace(group.QaName))
        {
            var finding = await ValidatePersonAsync(group, "QA", group.QaName, group.QaLine,
                cargos, personCache, cancellationToken);
            if (finding != null) blockFindings.Add(finding);
        }

        // Fase 4: pertenencia al grupo de desarrollo. Solo Warning, para no bloquear
        // la planificación mensual mientras se depuran los datos. No contradice la regla de
        // que una persona este en un unico grupo: alli el aviso es por curacion y aqui la
        // asignacion en si es un error duro.
        if (project != null && project.IsActive)
        {
            blockFindings.AddRange(await ValidateDevGroupMembershipAsync(group, project,
                personCache, cancellationToken));
        }

        return new ImportBlockPreviewDto
        {
            Index = group.Index,
            StartLine = group.StartLine,
            ProjectName = group.ProjectName,
            DevName = group.DevName,
            TeamLeadName = group.TeamLeadName,
            QaName = group.QaName,
            IsQaOnly = group.IsQaOnly,
            TasksCount = group.Tasks.Count,
            IsValid = blockFindings.All(f => f.Severity != ImportFindingSeverity.Error),
            Findings = blockFindings
        };
    }

    /// <summary>
    /// Comprueba que el Dev y el QA del bloque pertenezcan al grupo de desarrollo del
    /// proyecto. Si el proyecto no tiene grupo asignado no se dice nada: es un dato
    /// pendiente de curación, no un error del TXT.
    /// </summary>
    private async Task<List<ImportFindingDto>> ValidateDevGroupMembershipAsync(
        ParsedTaskGroupDto group,
        Project project,
        Dictionary<string, List<Person>> personCache,
        CancellationToken cancellationToken)
    {
        var result = new List<ImportFindingDto>();

        if (!project.DevGroupId.HasValue)
        {
            return result;
        }

        var memberIds = (await _unitOfWork.DevGroupMembers.GetMemberIdsAsync(project.DevGroupId.Value)).ToHashSet();

        var groupName = await GetDevGroupNameAsync(project.DevGroupId.Value, cancellationToken);

        await AddMembershipFindingAsync(group, "Dev", group.DevName, group.DevLine,
            memberIds, groupName, result, personCache);

        await AddMembershipFindingAsync(group, "QA", group.QaName, group.QaLine,
            memberIds, groupName, result, personCache);

        return result;
    }

    private async Task AddMembershipFindingAsync(
        ParsedTaskGroupDto group,
        string cargo,
        string? rawName,
        int line,
        HashSet<Guid> memberIds,
        string groupName,
        List<ImportFindingDto> target,
        Dictionary<string, List<Person>> personCache)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return;

        var name = rawName.Trim();

        if (!personCache.TryGetValue(name, out var matches))
        {
            matches = (await _unitOfWork.People.ResolvePersonCandidatesAsync(name)).ToList();
            personCache[name] = matches;
        }

        // Si la persona no existe, está inactiva o el nombre es ambiguo, ya hay un error
        // bloqueante reportado por ValidatePersonAsync: no se agrega ruido con un warning
        // derivado. Con varias coincidencias no se puede evaluar la pertenencia.
        if (matches.Count != 1) return;

        var person = matches[0];
        if (!person.IsActive) return;

        if (memberIds.Contains(person.Id)) return;

        target.Add(Warning(
            ImportFindingCode.PersonNotInDevGroup, line, group.Index, cargo, name,
            $"Bloque {group.Index}: '{name}' ({cargo}) no pertenece al grupo de desarrollo '{groupName}' del proyecto.",
            $"Agregue a '{name}' al grupo '{groupName}' en Grupos de Desarrollo."));
    }

    private async Task<string> GetDevGroupNameAsync(Guid devGroupId, CancellationToken cancellationToken)
    {
        var devGroup = await _unitOfWork.DevGroups.GetByIdAsync(devGroupId);
        return devGroup?.Name ?? "(grupo eliminado)";
    }

    private async Task<ImportFindingDto?> ValidatePersonAsync(
        ParsedTaskGroupDto group,
        string cargo,
        string rawName,
        int line,
        IReadOnlyDictionary<string, Role?> cargos,
        Dictionary<string, List<Person>> personCache,
        CancellationToken cancellationToken)
    {
        var name = rawName.Trim();

        if (!personCache.TryGetValue(name, out var matches))
        {
            matches = (await _unitOfWork.People.ResolvePersonCandidatesAsync(name)).ToList();
            personCache[name] = matches;
        }

        if (matches.Count == 0)
        {
            return Error(
                ImportFindingCode.PersonNotFound, line, group.Index, cargo, name,
                $"Bloque {group.Index}: la persona '{name}' ({cargo}) no existe.",
                $"Cree a '{name}' en Personas antes de importar. Si la persona ya existe pero con "
                + "apellidos, escribala en el TXT como 'Nombre Apellidos' o con su usuario del "
                + "sistema, y la busqueda la encontrara.");
        }

        // El TXT solo trae un token por persona, así que si ese nombre corresponde a varias
        // personas no hay forma de saber a cuál pertenece el bloque. Es un error bloqueante
        // porque adivinar asignaría el trabajo a la persona equivocada.
        if (matches.Count > 1)
        {
            var candidates = string.Join(", ", matches.Select(DescribeCandidate));

            return Error(
                ImportFindingCode.PersonAmbiguous, line, group.Index, cargo, name,
                $"Bloque {group.Index}: '{name}' ({cargo}) corresponde a {matches.Count} personas: {candidates}.",
                "Escriba en el TXT el nombre completo (por ejemplo 'Angel Perez') o el usuario del "
                + "sistema (por ejemplo 'a.perez') de la persona correcta para desambiguar.");
        }

        var person = matches[0];

        if (!person.IsActive)
        {
            return Error(
                ImportFindingCode.PersonInactive, line, group.Index, cargo, name,
                $"Bloque {group.Index}: la persona '{name}' ({cargo}) está inactiva.",
                $"Reactive a '{name}' en Personas antes de importar.");
        }

        if (!cargos.TryGetValue(cargo, out var role) || role == null)
        {
            return Error(
                ImportFindingCode.MissingRole, line, group.Index, cargo, name,
                $"Bloque {group.Index}: el cargo '{cargo}' no está creado en el catálogo.",
                $"Cree el cargo '{cargo}' en Cargos antes de importar.");
        }

        var hasRole = await _unitOfWork.PersonRoles.HasRoleAsync(person.Id, role.Id);
        if (!hasRole)
        {
            return Error(
                ImportFindingCode.MissingRole, line, group.Index, cargo, name,
                $"Bloque {group.Index}: '{name}' no tiene el cargo '{cargo}'.",
                $"Asigne el cargo '{cargo}' a '{name}' en Personas antes de importar.");
        }

        return null;
    }

    private async Task<Dictionary<string, Role?>> LoadRequiredRolesAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Role?>(StringComparer.OrdinalIgnoreCase);

        foreach (var cargo in RequiredCargos)
        {
            result[cargo] = await _unitOfWork.Roles.GetByNameCaseInsensitiveAsync(cargo);
        }

        return result;
    }

    /// <summary>
    /// Los únicos cargos que el TXT puede exigir. El catálogo admite otros, pero agregar
    /// uno nuevo no cambia el formato del archivo.
    /// </summary>
    private static readonly string[] RequiredCargos = { CatalogNames.Dev, CatalogNames.TeamLead, CatalogNames.Qa };

    internal static class CatalogNames
    {
        public const string Dev = "Dev";
        public const string TeamLead = "Team Lead";
        public const string Qa = "QA";
    }

    private static ImportFindingDto Error(ImportFindingCode code, int line, int blockIndex, string? field,
        string? value, string message, string? resolution) =>
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

    private static ImportFindingDto Warning(ImportFindingCode code, int line, int blockIndex, string? field,
        string? value, string message, string? resolution) =>
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

    /// <summary>
    /// Como se muestra un candidato cuando un nombre es ambiguo: nombre completo y, si la
    /// persona tiene, su usuario del sistema. Ese usuario es la clave natural para que el
    /// usuario del TXT desambigue sin conocer el indice (LastName, Name).
    /// </summary>
    private static string DescribeCandidate(Person person)
    {
        var display = PersonDisplayName.For(person.Name, person.LastName);
        return string.IsNullOrWhiteSpace(person.UserName)
            ? $"'{display}'"
            : $"'{display}' (usuario: {person.UserName})";
    }
}
