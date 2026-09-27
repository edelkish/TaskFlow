namespace TaskFlow.Domain.Entities;

/// <summary>
/// Catálogo de cargos del grupo de desarrollo (Dev, Team Lead, QA y otros).
/// Es una tabla y no un enum para permitir agregar cargos sin migrar.
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<PersonRole> PersonRoles { get; set; } = new List<PersonRole>();
}
