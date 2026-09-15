namespace TaskFlow.Domain.Entities;

/// <summary>
/// Configuración del sistema en pares clave-valor (ej.: tema de la UI).
/// </summary>
public class AppSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}