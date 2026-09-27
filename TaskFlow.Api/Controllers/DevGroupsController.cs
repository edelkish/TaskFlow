using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Authorization;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevGroupsController : ControllerBase
{
    private readonly IDevGroupService _devGroupService;

    public DevGroupsController(IDevGroupService devGroupService)
    {
        _devGroupService = devGroupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _devGroupService.GetAllAsync();
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _devGroupService.GetAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Create([FromBody] CreateDevGroupDto dto)
    {
        var result = await _devGroupService.CreateAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDevGroupDto dto)
    {
        var result = await _devGroupService.UpdateAsync(id, dto);
        if (!result.IsSuccess)
        {
            return result.Error!.Contains("no encontrado", StringComparison.OrdinalIgnoreCase)
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/members")]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> SetMembers(Guid id, [FromBody] SetDevGroupMembersDto dto)
    {
        var result = await _devGroupService.SetMembersAsync(id, dto.PersonIds);
        if (!result.IsSuccess)
        {
            return result.Error!.Contains("no encontrado", StringComparison.OrdinalIgnoreCase)
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Borrar un grupo no borra proyectos: la FK es ON DELETE SET NULL y el proyecto
    /// queda sin grupo, que es un estado válido.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _devGroupService.DeleteAsync(id);
        if (!result.IsSuccess)
        {
            return result.Error!.Contains("no encontrado", StringComparison.OrdinalIgnoreCase)
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return NoContent();
    }
}
