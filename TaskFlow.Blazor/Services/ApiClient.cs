using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Blazor.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task<HttpResponseMessage> EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return response;

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var statusCode = (int)response.StatusCode;

        // Los controllers devuelven BadRequest(result.Error), y al ser un string el
        // serializador lo envuelve como literal JSON: el cuerpo llega como
        // "\"No se puede eliminar...\"" con comillas. Se desenvuelve para que en el toast
        // se lea la frase, y solo se antepone el codigo cuando no hay mensaje que mostrar.
        var detail = TryUnwrapJsonString(body) ?? body.Trim();
        var message = string.IsNullOrWhiteSpace(detail)
            ? $"HTTP {statusCode}"
            : detail;

        throw new HttpRequestException(message);
    }

    private static string? TryUnwrapJsonString(string body)
    {
        var trimmed = body.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '"' || trimmed[^1] != '"')
            return null;

        try
        {
            return JsonSerializer.Deserialize<string>(trimmed);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Auth
    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
    }

    // Projects
    public async Task<List<ProjectDto>?> GetProjectsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ProjectDto>>("api/projects", _jsonOptions);
    }

    public async Task<ProjectDto?> GetProjectAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<ProjectDto>($"api/projects/{id}", _jsonOptions);
    }

    public async Task<ProjectDto?> CreateProjectAsync(CreateProjectDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/projects", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ProjectDto>(_jsonOptions);
    }

    public async Task<ProjectDto?> UpdateProjectAsync(Guid id, UpdateProjectDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/projects/{id}", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ProjectDto>(_jsonOptions);
    }

    public async Task DeleteProjectAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/projects/{id}");
        response = await EnsureSuccessAsync(response);
    }

    // Importación
    public async Task<ImportResultDto?> ImportFileAsync(string filePath)
    {
        var response = await _httpClient.PostAsJsonAsync("api/import", new { filePath });
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ImportResultDto>(_jsonOptions);
    }

    /// <summary>
    /// Previsualización en seco. Devuelve 200 con el detalle incluso si el archivo es
    /// inválido, porque el resultado de la validación es la respuesta esperada.
    /// </summary>
    public async Task<ImportValidationDto?> ValidateImportFileAsync(string filePath)
    {
        var response = await _httpClient.PostAsJsonAsync("api/import/validate", new { filePath });
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ImportValidationDto>(_jsonOptions);
    }

    /// <summary>Intenta importar y captura el rechazo estructurado si el archivo no es válido.</summary>
    public async Task<ImportAttemptDto> TryImportFileAsync(string filePath)
    {
        var response = await _httpClient.PostAsJsonAsync("api/import", new { filePath });

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var rejection = await response.Content.ReadFromJsonAsync<ImportRejectionDto>(_jsonOptions);
            return new ImportAttemptDto { Rejection = rejection };
        }

        response = await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>(_jsonOptions);
        return new ImportAttemptDto { Result = result };
    }

    public async Task<List<ImportBatchDto>?> GetImportBatchesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ImportBatchDto>>("api/import", _jsonOptions);
    }

    // Periodos
    public async Task<List<PeriodDto>?> GetPeriodsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<PeriodDto>>("api/periods", _jsonOptions);
    }

    public async Task<PeriodDto?> CreatePeriodAsync(CreatePeriodDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/periods", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PeriodDto>(_jsonOptions);
    }

    public async Task<PeriodDto?> UpdatePeriodAsync(Guid id, UpdatePeriodDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/periods/{id}", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PeriodDto>(_jsonOptions);
    }

    public async Task DeletePeriodAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/periods/{id}");
        await EnsureSuccessAsync(response);
    }

    // Grupos de tareas
    public async Task<List<TaskGroupDto>?> GetGroupsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<TaskGroupDto>>("api/taskgroups", _jsonOptions);
    }

    public async Task<List<TaskGroupDto>?> GetGroupsByPeriodAsync(Guid periodId)
    {
        return await _httpClient.GetFromJsonAsync<List<TaskGroupDto>>($"api/taskgroups/period/{periodId}", _jsonOptions);
    }

    public async Task<TaskGroupDto?> GetGroupAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<TaskGroupDto>($"api/taskgroups/{id}", _jsonOptions);
    }

    public async Task<TaskGroupDto?> CreateGroupAsync(CreateTaskGroupDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/taskgroups", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TaskGroupDto>(_jsonOptions);
    }

    public async Task<TaskGroupDto?> UpdateGroupAsync(Guid id, UpdateTaskGroupDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/taskgroups/{id}", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TaskGroupDto>(_jsonOptions);
    }

    public async Task DeleteGroupAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/taskgroups/{id}");
        response = await EnsureSuccessAsync(response);
    }

    // Tareas de planificación
    public async Task<List<PlanningTaskDto>?> GetTasksByGroupAsync(Guid taskGroupId)
    {
        return await _httpClient.GetFromJsonAsync<List<PlanningTaskDto>>($"api/planningtasks/group/{taskGroupId}", _jsonOptions);
    }

    public async Task<PlanningTaskDto?> GetTaskAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<PlanningTaskDto>($"api/planningtasks/{id}", _jsonOptions);
    }

    public async Task<PlanningTaskDto?> CreateTaskAsync(CreatePlanningTaskDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/planningtasks", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PlanningTaskDto>(_jsonOptions);
    }

    public async Task<PlanningTaskDto?> UpdateTaskAsync(Guid id, UpdatePlanningTaskDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/planningtasks/{id}", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PlanningTaskDto>(_jsonOptions);
    }

    public async Task<PlanningTaskDto?> ReassignTaskAsync(Guid id, Guid? personId)
    {
        var url = personId.HasValue
            ? $"api/planningtasks/{id}/assign/{personId.Value}"
            : $"api/planningtasks/{id}/assign";
        var response = await _httpClient.PatchAsync(url, null);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PlanningTaskDto>(_jsonOptions);
    }

    public async Task DeleteTaskFromGroupAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/planningtasks/{id}");
        response = await EnsureSuccessAsync(response);
    }

    // Backlog: tareas sin grupo (PlanningTask.TaskGroupId == null)
    public async Task<List<PlanningTaskDto>?> GetBacklogAsync(Guid? projectId = null, Guid? assigneeId = null)
    {
        var query = new List<string>();
        if (projectId.HasValue) query.Add($"projectId={projectId.Value}");
        if (assigneeId.HasValue) query.Add($"assigneeId={assigneeId.Value}");

        var url = "api/planningtasks/backlog";
        if (query.Count > 0) url += "?" + string.Join("&", query);

        return await _httpClient.GetFromJsonAsync<List<PlanningTaskDto>>(url, _jsonOptions);
    }

    // Personas
    public async Task<List<PersonDto>?> GetPeopleAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<PersonDto>>("api/people", _jsonOptions);
    }

    public async Task<PersonDto?> CreatePersonAsync(CreatePersonDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/people", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PersonDto>(_jsonOptions);
    }

    public async Task<PersonDto?> UpdatePersonAsync(Guid id, UpdatePersonDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/people/{id}", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PersonDto>(_jsonOptions);
    }

    public async Task DeletePersonAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/people/{id}");
        response = await EnsureSuccessAsync(response);
    }

    public async Task<PersonDto?> SetPersonRolesAsync(Guid id, SetPersonRolesDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/people/{id}/roles", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<PersonDto>(_jsonOptions);
    }

    // Cargos
    public async Task<List<RoleDto>?> GetRolesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<RoleDto>>("api/roles", _jsonOptions);
    }

    public async Task<RoleDto?> CreateRoleAsync(CreateRoleDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/roles", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RoleDto>(_jsonOptions);
    }

    public async Task<RoleDto?> UpdateRoleAsync(Guid id, UpdateRoleDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/roles/{id}", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RoleDto>(_jsonOptions);
    }

    public async Task DeleteRoleAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/roles/{id}");
        response = await EnsureSuccessAsync(response);
    }

    // Grupos de desarrollo
    public async Task<List<DevGroupDto>?> GetDevGroupsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<DevGroupDto>>("api/devgroups", _jsonOptions);
    }

    public async Task<DevGroupDto?> GetDevGroupAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<DevGroupDto>($"api/devgroups/{id}", _jsonOptions);
    }

    public async Task<DevGroupDto?> CreateDevGroupAsync(CreateDevGroupDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/devgroups", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<DevGroupDto>(_jsonOptions);
    }

    public async Task<DevGroupDto?> UpdateDevGroupAsync(Guid id, UpdateDevGroupDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/devgroups/{id}", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<DevGroupDto>(_jsonOptions);
    }

    public async Task<DevGroupDto?> SetDevGroupMembersAsync(Guid id, List<Guid> personIds)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/devgroups/{id}/members", new SetDevGroupMembersDto { PersonIds = personIds });
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<DevGroupDto>(_jsonOptions);
    }

    public async Task DeleteDevGroupAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/devgroups/{id}");
        response = await EnsureSuccessAsync(response);
    }

    // Configuración del sistema
    public async Task<ThemeSettingsDto?> GetThemeSettingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<ThemeSettingsDto>("api/settings", _jsonOptions);
    }

    public async Task<ThemeSettingsDto?> UpdateThemeSettingsAsync(UpdateThemeSettingsDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync("api/settings", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ThemeSettingsDto>(_jsonOptions);
    }
}
