using AutoMapper;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class ImportService : IImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImportValidator _validator;
    private readonly ITaskFileParser _parser;
    private readonly IMapper _mapper;

    public ImportService(IUnitOfWork unitOfWork, IImportValidator validator,
        ITaskFileParser parser, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _parser = parser;
        _mapper = mapper;
    }

    public Task<ImportValidationDto> ValidateAsync(string filePath, CancellationToken cancellationToken = default) =>
        _validator.ValidateAsync(filePath, cancellationToken);

    public async Task<ImportExecutionDto> ImportTaskFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(filePath, cancellationToken);

        if (!validation.IsValid)
        {
            var rejectedBatchId = await RecordRejectionAsync(validation, cancellationToken);
            return new ImportExecutionDto
            {
                Accepted = false,
                Validation = validation,
                RejectedBatchId = rejectedBatchId
            };
        }

        var parsed = await _parser.ParseAsync(filePath, cancellationToken);

        // La validación garantiza que el periodo existe; sin este guardia, unalguna
        // inconsistencia entre ambas consultas produciría un NullReferenceException.
        var period = await _unitOfWork.Periods.GetByMonthYearAsync(parsed.Month, parsed.Year)
                     ?? throw new InvalidOperationException(
                         $"El periodo '{parsed.PeriodName}' dejó de existir entre la validación y la importación.");

        // El lote queda Partial si hubo cualquier advertencia, venga del parser (líneas raras)
        // o del validador (Dev fuera del grupo de desarrollo, proyecto inactivo...). Mirar
        // solo parsed.Findings dejaba los warnings del validador invisibles en el historial.
        var findings = parsed.Findings.Concat(validation.Findings).ToList();

        var result = new ImportResultDto
        {
            PeriodName = parsed.PeriodName,
            Warnings = findings.Select(f => f.Message).ToList()
        };

        // Todo el lote es atómico: si algo falla a mitad de camino, el rollback deja
        // planificación, grupos y tareas exactamente como estaban.
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var batch = new ImportBatch
            {
                PeriodId = period.Id,
                FileName = parsed.FileName,
                FilePath = parsed.FilePath,
                FileEncoding = parsed.FileEncoding,
                Status = findings.Count > 0 ? ImportStatus.Partial : ImportStatus.Success,
                SectionsCount = parsed.Groups.Count,
                WarningsCount = findings.Count,
                Note = findings.Count > 0 ? Summarize(findings) : null,
                ImportedAt = DateTime.UtcNow
            };

            await _unitOfWork.ImportBatches.AddAsync(batch);

            foreach (var group in parsed.Groups)
            {
                var (created, removed, imported) = await ApplyGroupAsync(period, group, batch, cancellationToken);
                if (created) result.GroupsCreated++; else result.GroupsUpdated++;
                result.TasksImported += imported;
                result.TasksRemoved += removed;
            }

            batch.TasksCount = result.TasksImported;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            result.ImportBatchId = batch.Id;
            return new ImportExecutionDto
            {
                Accepted = true,
                Validation = validation,
                Result = result
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Un intento rechazado no toca planificación, grupos ni tareas, pero sí queda
    /// registrado como ImportStatus.Failed para poder auditar quién intentó qué y por qué
    /// se rechazó. El periodo puede quedar null: el rechazo más común es precisamente
    /// un periodo inexistente.
    /// </summary>
    private async Task<Guid?> RecordRejectionAsync(ImportValidationDto validation, CancellationToken cancellationToken)
    {
        try
        {
            var batch = new ImportBatch
            {
                PeriodId = validation.PeriodId,
                FileName = validation.FileName,
                FilePath = validation.FilePath,
                FileEncoding = validation.FileEncoding,
                Status = ImportStatus.Failed,
                SectionsCount = validation.BlocksCount,
                TasksCount = 0,
                WarningsCount = validation.WarningsCount,
                Note = Summarize(validation.Findings),
                ImportedAt = DateTime.UtcNow
            };

            await _unitOfWork.ImportBatches.AddAsync(batch);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return batch.Id;
        }
        catch
        {
            // Red de seguridad para fallos de infraestructura (base no disponible, etc.).
            // Auditar el intento es deseable, pero nunca debe convertir un rechazo ya
            // decidido en un error 500: el usuario igual no va a importar nada. Un
            // ImportBatch null aquí significa que ni siquiera el registro del intento
            // fallido se pudo guardar.
            return null;
        }
    }

    private async Task<(bool Created, int Removed, int Imported)> ApplyGroupAsync(
        Period period,
        ParsedTaskGroupDto group,
        ImportBatch batch,
        CancellationToken cancellationToken)
    {
        var dev = await ResolvePersonAsync(group.DevName);
        var teamLead = await ResolvePersonAsync(group.TeamLeadName);
        var qa = await ResolvePersonAsync(group.QaName);

        // La validación ya garantizó que el proyecto existe; aquí solo se busca.
        Project? project = null;
        if (!string.IsNullOrWhiteSpace(group.ProjectName))
        {
            project = await _unitOfWork.Projects.GetByNameCaseInsensitiveAsync(group.ProjectName.Trim());
        }

        TaskGroup? taskGroup;

        // La clave de idempotencia depende de qué declara el bloque. Hay tres combinaciones
        // posibles y cada una necesita su propia clave: si se usara la clave QA-only para un
        // bloque que sí tiene App:, la búsqueda exigiría ProjectId NULL, no encontraría el
        // grupo existente y cada reimportación crearía un duplicado.
        if (project != null && dev != null)
        {
            taskGroup = await _unitOfWork.TaskGroups.GetByDevBlockKeyAsync(period.Id, project.Id, dev.Id);
        }
        else if (project != null && qa != null)
        {
            taskGroup = await _unitOfWork.TaskGroups.GetByQaWithProjectBlockKeyAsync(period.Id, project.Id, qa.Id);
        }
        else if (qa != null)
        {
            taskGroup = await _unitOfWork.TaskGroups.GetByQaBlockKeyAsync(period.Id, qa.Id);
        }
        else
        {
            taskGroup = null;
        }

        var created = taskGroup == null;

        if (taskGroup == null)
        {
            taskGroup = new TaskGroup
            {
                PeriodId = period.Id,
                ImportBatchId = batch.Id,
                ProjectId = project?.Id,
                DevPersonId = dev?.Id,
                TeamLeadPersonId = teamLead?.Id,
                QaPersonId = qa?.Id,
                IsActive = true
            };
            await _unitOfWork.TaskGroups.AddAsync(taskGroup);
        }
        else
        {
            taskGroup.ImportBatchId = batch.Id;
            taskGroup.PeriodId = period.Id;
            taskGroup.ProjectId = project?.Id;
            taskGroup.DevPersonId = dev?.Id;
            taskGroup.TeamLeadPersonId = teamLead?.Id;
            taskGroup.QaPersonId = qa?.Id;
        }

        // Semántica Reemplazar: se eliminan las tareas de origen importadas (se conservan manuales).
        int removed = await _unitOfWork.PlanningTasks.DeleteByGroupWhereImportAsync(taskGroup.Id);

        // Auto-asignación: Dev del bloque, o QA en bloques QA-only.
        var assignee = dev ?? qa;

        // El proyecto es NULL solo en bloques QA-only, que por definición no declaran App:.
        foreach (var task in group.Tasks)
        {
            await _unitOfWork.PlanningTasks.AddAsync(new PlanningTask
            {
                TaskGroupId = taskGroup.Id,
                ProjectId = project?.Id,
                Number = task.Number,
                SubNumber = task.SubNumber,
                Description = task.Description,
                AssignedPersonId = assignee?.Id,
                Source = TaskSource.Import,
                IsActive = true
            });
        }

        return (created, removed, group.Tasks.Count);
    }

    /// <summary>
    /// Busca una persona ya existente. A diferencia de la versión anterior, nunca crea
    /// una nueva: las personas son un maestro curado y la importación no inventa datos.
    /// </summary>
    private async Task<Person?> ResolvePersonAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var matches = await _unitOfWork.People.GetAllByNameCaseInsensitiveAsync(name.Trim());

        // La validación previa ya bloquea el archivo ante nombres ambiguos, así que llegar
        // aquí con más de una coincidencia significa que se saltó esa validación. Se lanza
        // una excepción en vez de escoger la primera porque la importación es todo-o-nada:
        // asignar el bloque a una persona arbitraria sería peor que fallar.
        if (matches.Count > 1)
        {
            var candidates = string.Join(", ", matches
                .Select(m => PersonDisplayName.For(m.Name, m.LastName)));

            throw new InvalidOperationException(
                $"La persona '{name.Trim()}' es ambigua: corresponde a {matches.Count} personas ({candidates}). "
                + "La importación debe pasar antes por la validación.");
        }

        return matches.Count == 0 ? null : matches[0];
    }

    private static string Summarize(IEnumerable<ImportFindingDto> findings)
    {
        var ordered = findings
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.Line)
            .ToList();

        return string.Join(Environment.NewLine, ordered.Select(f =>
            f.Line > 0 ? $"[L{f.Line}] {f.Message}" : f.Message));
    }

    public async Task<IEnumerable<ImportBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default)
    {
        var batches = await _unitOfWork.ImportBatches.GetOrderedDescAsync();
        return _mapper.Map<IEnumerable<ImportBatchDto>>(batches);
    }

    public async Task<ImportBatchDto?> GetBatchAsync(Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var batch = await _unitOfWork.ImportBatches.GetWithGroupsAsync(importBatchId);
        return batch == null ? null : _mapper.Map<ImportBatchDto>(batch);
    }

    public async Task<IEnumerable<TaskGroupDto>> GetGroupsByPeriodAsync(Guid periodId, CancellationToken cancellationToken = default)
    {
        var groups = await _unitOfWork.TaskGroups.GetByPeriodAsync(periodId);
        return _mapper.Map<IEnumerable<TaskGroupDto>>(groups);
    }

    public async Task<TaskGroupDto?> GetGroupAsync(Guid taskGroupId, CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.TaskGroups.GetWithTasksAsync(taskGroupId);
        return group == null ? null : _mapper.Map<TaskGroupDto>(group);
    }
}
