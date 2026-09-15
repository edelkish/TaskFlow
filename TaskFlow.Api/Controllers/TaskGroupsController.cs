using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskGroupsController : ControllerBase
{
    private readonly ITaskGroupService _taskGroupService;

    public TaskGroupsController(ITaskGroupService taskGroupService)
    {
        _taskGroupService = taskGroupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _taskGroupService.GetAllAsync();
        return Ok(result.Value);
    }

    [HttpGet("period/{periodId:guid}")]
    public async Task<IActionResult> GetByPeriod(Guid periodId)
    {
        var result = await _taskGroupService.GetByPeriodAsync(periodId);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _taskGroupService.GetAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskGroupDto dto)
    {
        var result = await _taskGroupService.CreateAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskGroupDto dto)
    {
        var result = await _taskGroupService.UpdateAsync(id, dto);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _taskGroupService.DeleteAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return NoContent();
    }
}