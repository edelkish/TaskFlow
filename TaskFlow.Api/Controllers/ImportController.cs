using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Authorization;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly IImportService _importService;

    public ImportController(IImportService importService)
    {
        _importService = importService;
    }

    /// <summary>
    /// Previsualización en seco. Devuelve 200 incluso con errores, porque el resultado de
    /// la validación es la respuesta esperada, no un fallo de la llamada.
    /// </summary>
    [HttpPost("validate")]
    [Authorize(Policy = AppPolicies.ImportWrite)]
    public async Task<IActionResult> ValidateFile([FromBody] ImportFileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return BadRequest("Debe indicar la ruta del archivo TXT.");

        if (!System.IO.File.Exists(request.FilePath))
            return BadRequest($"No se encontró el archivo en la ruta indicada: '{request.FilePath}'.");

        var validation = await _importService.ValidateAsync(request.FilePath);
        return Ok(validation);
    }

    /// <summary>
    /// Importa el archivo. Si la validación encuentra un solo desconocido, responde 409
    /// con el detalle y no modifica nada.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.ImportWrite)]
    public async Task<IActionResult> ImportFile([FromBody] ImportFileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return BadRequest("Debe indicar la ruta del archivo TXT.");

        if (!System.IO.File.Exists(request.FilePath))
            return BadRequest($"No se encontró el archivo en la ruta indicada: '{request.FilePath}'.");

        var execution = await _importService.ImportTaskFileAsync(request.FilePath);

        if (!execution.Accepted)
        {
            return Conflict(new ImportRejectionDto
            {
                Message = execution.Validation.Summary,
                Validation = execution.Validation,
                RejectedBatchId = execution.RejectedBatchId
            });
        }

        return Ok(execution.Result);
    }

    [HttpGet]
    public async Task<IActionResult> GetBatches()
    {
        var batches = await _importService.GetBatchesAsync();
        return Ok(batches);
    }

    [HttpGet("batches/{id:guid}")]
    public async Task<IActionResult> GetBatch(Guid id)
    {
        var batch = await _importService.GetBatchAsync(id);
        if (batch == null)
            return NotFound("Lote de importación no encontrado.");

        return Ok(batch);
    }
}

public record ImportFileRequest
{
    public string FilePath { get; init; } = string.Empty;
}
