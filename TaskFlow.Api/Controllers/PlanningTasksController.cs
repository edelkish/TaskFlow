using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Authorization;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanningTasksController : ControllerBase
{
    private readonly IPlanningTaskService _planningTaskService;

    public PlanningTasksController(IPlanningTaskService planningTaskService)
    {
        _planningTaskService = planningTaskService;
    }

    [HttpGet("group/{taskGroupId:guid}")]
    public async Task<IActionResult> GetByGroup(Guid taskGroupId)
    {
        var result = await _planningTaskService.GetByGroupAsync(taskGroupId);
        return Ok(result.Value);
    }

    /// <summary>
    /// Backlog: tareas sin grupo (TaskGroupId NULL). Los filtros son opcionales y se
    /// combinan, por eso no hay ruta por defecto que los pise.
    /// </summary>
    [HttpGet("backlog")]
    public async Task<IActionResult> GetBacklog([FromQuery] Guid? projectId, [FromQuery] Guid? assigneeId)
    {
        var result = await _planningTaskService.GetBacklogAsync(projectId, assigneeId);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _planningTaskService.GetAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Create([FromBody] CreatePlanningTaskDto dto)
    {
        var result = await _planningTaskService.CreateAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanningTaskDto dto)
    {
        var result = await _planningTaskService.UpdateAsync(id, dto);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/assign/{personId:guid?}")]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Reassign(Guid id, Guid? personId)
    {
        var result = await _planningTaskService.ReassignAsync(id, personId);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _planningTaskService.DeleteAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return NoContent();
    }
}