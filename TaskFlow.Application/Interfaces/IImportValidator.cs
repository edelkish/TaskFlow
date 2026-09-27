using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces;

public interface IImportValidator
{
    /// <summary>
    /// Analiza el archivo contra los maestros existentes y devuelve todos los hallazgos.
    /// No escribe nada: es seguro invocarlo como previsualización.
    /// </summary>
    Task<ImportValidationDto> ValidateAsync(string filePath, CancellationToken cancellationToken = default);
}
