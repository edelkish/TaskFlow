namespace TaskFlow.Domain.Entities;

/// <summary>
/// Membresía de una persona en un grupo de desarrollo. No tiene identidad propia: su
/// clave es compuesta (DevGroupId, PersonId). No extiende BaseEntity por eso.
/// </summary>
public class DevGroupMember
{
    public Guid DevGroupId { get; set; }
    public Guid PersonId { get; set; }

    // Navigation properties
    public DevGroup DevGroup { get; set; } = null!;
    public Person Person { get; set; } = null!;
}
