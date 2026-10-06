using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface IPeriodRepository : IGenericRepository<Period>
{
    Task<Period?> GetByMonthYearAsync(int month, int year);

    /// <summary>
    /// Un periodo con sus colecciones cargadas, para que los contadores del DTO no salgan
    /// en cero. Sin esto GetAsync devolveria GroupCount e ImportCount a 0 aunque el periodo
    /// tenga contenido, porque las colecciones llegan vacias en lugar de con datos.
    /// </summary>
    Task<Period?> GetWithRelatedAsync(Guid id);

    /// <summary>
    /// Periodos del mas reciente al mas antiguo, con sus colecciones de grupos e
    /// importaciones cargadas para poder contarlas en memoria. Se.Include en una sola
    /// consulta en lugar de un conteo por periodo, que seria un N+1.
    /// </summary>
    Task<IReadOnlyList<Period>> GetOrderedDescWithRelatedAsync();

    /// <summary>
    /// Cuantos grupos de tareas cuelgan del periodo. Las tareas llegan por aqui:
    /// Period -> TaskGroup -> PlanningTask, no hay FK directa de la tarea al periodo.
    /// </summary>
    Task<int> CountTaskGroupsAsync(Guid periodId);

    /// <summary>
    /// Cuantas importaciones se hicieron para el periodo. La FK esta en Restrict, asi que
    /// esto tambien impide el borrado aunque el periodo no tenga grupos.
    /// </summary>
    Task<int> CountImportBatchesAsync(Guid periodId);
}

public interface IPersonRepository : IGenericRepository<Person>
{
    /// <summary>
    /// Personas que coinciden con el texto que escribe alguien en el TXT, sin distinguir
    /// mayusculas ni acentos (las tres columnas llevan collation CI_AI). El texto puede
    /// ser el nombre solo ("Angel"), el usuario del sistema ("a.perez") o el nombre
    /// completo ("Angel Perez"), y se buscan los tres en una sola consulta.
    ///
    /// Puede devolver mas de una: el nombre solo ya no es clave unica, y quien llama debe
    /// detectar la ambiguedad en lugar de quedarse con la primera.
    /// </summary>
    Task<IReadOnlyList<Person>> ResolvePersonCandidatesAsync(string raw);

    /// <summary>Indica si ya existe una persona con ese nombre y apellidos.</summary>
    Task<bool> ExistsWithNameAndLastNameAsync(string name, string? lastName, Guid? excludingId = null);

    /// <summary>Indica si el usuario ya está tomado por otra persona.</summary>
    Task<bool> ExistsWithUserNameAsync(string? userName, Guid? excludingId = null);
}