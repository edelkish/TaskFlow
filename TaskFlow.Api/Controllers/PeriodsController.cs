using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Authorization;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeriodsController : ControllerBase
{
    private readonly IPeriodService _periodService;

    public PeriodsController(IPeriodService periodService)
    {
        _periodService = periodService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _periodService.GetAllAsync();
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _periodService.GetAsync(id);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Create([FromBody] CreatePeriodDto dto)
    {
        var result = await _periodService.CreateAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Solo actualiza el nombre. Anio y mes se ignoran aunque venir en el cuerpo: son la
    /// identidad del periodo y moverlos reubicaria sus grupos y tareas.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePeriodDto dto)
    {
        var result = await _periodService.UpdateAsync(id, dto);
        if (!result.IsSuccess)
        {
            return result.Error!.Contains("no encontrado", StringComparison.OrdinalIgnoreCase)
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Un periodo con grupos o importaciones no se borra: el servicio responde 400 con el
    /// detalle de cuanto hay que quitar antes. Las tareas del mes son historico de
    /// planificacion, asi que la decision de borrarlas o reubicarlas es del usuario.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.MasterDataWrite)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _periodService.DeleteAsync(id);
        if (!result.IsSuccess)
        {
            return result.Error!.Contains("no encontrado", StringComparison.OrdinalIgnoreCase)
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return NoContent();
    }
}