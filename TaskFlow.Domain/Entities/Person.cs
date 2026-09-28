namespace TaskFlow.Domain.Entities;

/// <summary>
/// Persona del grupo de desarrollo normalizada. Sus cargos se asignan en
/// <see cref="PersonRole"/>: la misma persona puede tener varios.
/// </summary>
public class Person : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Apellidos. Opcional: el TXT de planificacion solo trae un token por persona, asi
    /// que hay altas y filas historicas sin apellidos.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Usuario corporativo de la persona. Opcional, y distinto de <see cref="UserId"/>:
    /// este es el login legible que aparece en la interfaz, aquel es el GUID de
    /// AspNetUsers con el que se vincula al login de Identity.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>Identificador del usuario Identity vinculado (AspNetUsers.Id).</summary>
    public string? UserId { get; set; }

    // Navigation properties
    public ICollection<PersonRole> PersonRoles { get; set; } = new List<PersonRole>();
    public ICollection<DevGroupMember> DevGroupMemberships { get; set; } = new List<DevGroupMember>();

    // Bloques mensuales del TXT en los que participa por cada rol. Son TaskGroup
    // (bloque del TXT), no grupos de desarrollo: ver DevGroup.
    public ICollection<TaskGroup> PeriodGroupsAsDev { get; set; } = new List<TaskGroup>();
    public ICollection<TaskGroup> PeriodGroupsAsTeamLead { get; set; } = new List<TaskGroup>();
    public ICollection<TaskGroup> PeriodGroupsAsQa { get; set; } = new List<TaskGroup>();
    public ICollection<PlanningTask> AssignedTasks { get; set; } = new List<PlanningTask>();
}
