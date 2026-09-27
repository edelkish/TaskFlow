namespace TaskFlow.Domain.Entities;

/// <summary>
/// Asignación de un cargo a una persona. N-a-N porque la misma persona puede
/// tener varios cargos (por ejemplo Dev y Team Lead en el mismo mes).
/// Tabla puente con clave primaria compuesta, sin identidad propia.
/// </summary>
public class PersonRole
{
    public Guid PersonId { get; set; }
    public Guid RoleId { get; set; }

    // Navigation properties
    public Person Person { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
