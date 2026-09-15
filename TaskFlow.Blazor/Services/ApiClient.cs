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
        var message = string.IsNullOrWhiteSpace(body)
            ? $"HTTP {statusCode}"
            : $"HTTP {statusCode}: {body.Trim()}";

        throw new HttpRequestException(message);
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

    // Tasks
    public async Task<List<TaskDto>?> GetTasksByProjectAsync(Guid projectId)
    {
        return await _httpClient.GetFromJsonAsync<List<TaskDto>>($"api/tasks/project/{projectId}", _jsonOptions);
    }

    public async Task<TaskDto?> CreateTaskAsync(CreateTaskDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tasks", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TaskDto>(_jsonOptions);
    }

    public async Task<TaskDto?> UpdateTaskAsync(Guid id, UpdateTaskDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tasks/{id}", dto);
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TaskDto>(_jsonOptions);
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/tasks/{id}");
        response = await EnsureSuccessAsync(response);
    }

    // Importación
    public async Task<ImportResultDto?> ImportFileAsync(string filePath)
    {
        var response = await _httpClient.PostAsJsonAsync("api/import", new { filePath });
        response = await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ImportResultDto>(_jsonOptions);
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
}
