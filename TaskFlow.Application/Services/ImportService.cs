using AutoMapper;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Services;

public class ImportService : IImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITaskFileParser _parser;
    private readonly IMapper _mapper;

    public ImportService(IUnitOfWork unitOfWork, ITaskFileParser parser, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _parser = parser;
        _mapper = mapper;
    }

    public async Task<ImportResultDto> ImportTaskFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var parsed = await _parser.ParseAsync(filePath, cancellationToken);

        var result = new ImportResultDto { PeriodName = parsed.PeriodName };
        var notes = new List<string>(parsed.Warnings);

        if (parsed.Month == 0 || parsed.Year == 0)
        {
            return new ImportResultDto
            {
                PeriodName = parsed.PeriodName,
                Warnings = parsed.Warnings
            };
        }

        var period = await _unitOfWork.Periods.GetByMonthYearAsync(parsed.Month, parsed.Year);
        if (period == null)
        {
            period = new Period
            {
                Month = parsed.Month,
                Year = parsed.Year,
                Name = parsed.PeriodName,
                IsActive = true
            };
            await _unitOfWork.Periods.AddAsync(period);
        }

        var batch = new ImportBatch
        {
            PeriodId = period.Id,
            FileName = parsed.FileName,
            FilePath = parsed.FilePath,
            FileEncoding = parsed.FileEncoding,
            Status = parsed.Warnings.Count > 0 ? ImportStatus.Partial : ImportStatus.Success,
            SectionsCount = parsed.Groups.Count,
            WarningsCount = parsed.Warnings.Count,
            Note = parsed.Warnings.Count > 0 ? string.Join(Environment.NewLine, parsed.Warnings) : null,
            ImportedAt = DateTime.UtcNow
        };

        foreach (var group in parsed.Groups)
        {
            var (taskGroup, created, removed) = await ResolveTaskGroupAsync(period, group, batch);
            if (taskGroup != null)
            {
                if (created) result.GroupsCreated++; else result.GroupsUpdated++;
                result.TasksImported += group.Tasks.Count;
                result.TasksRemoved += removed;
            }
        }

        batch.TasksCount = result.TasksImported;
        await _unitOfWork.ImportBatches.AddAsync(batch);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        result.ImportBatchId = batch.Id;
        result.Warnings = parsed.Warnings;
        return result;
    }

    private async Task<(TaskGroup?, bool, int)> ResolveTaskGroupAsync(Period period, ParsedTaskGroupDto group, ImportBatch batch)
    {
        Person? dev = null;
        Person? teamLead = null;
        Person? qa = null;

        if (!string.IsNullOrWhiteSpace(group.DevName))
        {
            dev = await GetOrCreatePersonAsync(group.DevName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(group.TeamLeadName))
        {
            teamLead = await GetOrCreatePersonAsync(group.TeamLeadName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(group.QaName))
        {
            qa = await GetOrCreatePersonAsync(group.QaName.Trim());
        }

        Project? project = null;
        if (!string.IsNullOrWhiteSpace(group.ProjectName))
        {
            project = await _unitOfWork.Projects.GetByNameCaseInsensitiveAsync(group.ProjectName.Trim())
                       ?? await _unitOfWork.Projects.GetByNameAsync(group.ProjectName.Trim());
        }

        TaskGroup? taskGroup;
        bool created;

        if (project != null && dev != null)
        {
            taskGroup = await _unitOfWork.TaskGroups.GetByDevBlockKeyAsync(period.Id, project.Id, dev.Id);
        }
        else
        {
            taskGroup = qa != null
                ? await _unitOfWork.TaskGroups.GetByQaBlockKeyAsync(period.Id, qa.Id)
                : null;
        }

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
            created = true;
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
            created = false;
        }

        // Semántica Reemplazar: se eliminan las tareas de origen importadas (se conservan manuales).
        int removed = await _unitOfWork.PlanningTasks.DeleteByGroupWhereImportAsync(taskGroup.Id);

        // Auto-asignación: Dev del bloque, o QA en bloques QA-only.
        var assignee = dev ?? qa;

        foreach (var task in group.Tasks)
        {
            await _unitOfWork.PlanningTasks.AddAsync(new PlanningTask
            {
                TaskGroupId = taskGroup.Id,
                Number = task.Number,
                SubNumber = task.SubNumber,
                Description = task.Description,
                AssignedPersonId = assignee?.Id,
                Source = TaskSource.Import,
                IsActive = true
            });
        }

        return (taskGroup, created, removed);
    }

    private async Task<Person> GetOrCreatePersonAsync(string name)
    {
        var person = await _unitOfWork.People.GetByNameCaseInsensitiveAsync(name);
        if (person == null)
        {
            person = new Person { Name = name, IsActive = true };
            await _unitOfWork.People.AddAsync(person);
        }

        return person;
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