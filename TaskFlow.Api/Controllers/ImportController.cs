using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPost]
    public async Task<IActionResult> ImportFile([FromBody] ImportFileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return BadRequest("Debe indicar la ruta del archivo TXT.");

        var result = await _importService.ImportTaskFileAsync(request.FilePath);
        return Ok(result);
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