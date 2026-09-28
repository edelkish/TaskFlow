using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface IPeriodRepository : IGenericRepository<Period>
{
    Task<Period?> GetByMonthYearAsync(int month, int year);
    Task<IEnumerable<Period>> GetOrderedDescAsync();
}

public interface IPersonRepository : IGenericRepository<Person>
{
    /// <summary>
    /// Todas las personas cuyo nombre coincide, sin distinguir mayusculas ni acentos.
    /// Puede devolver mas de una: el nombre ya no es clave unica y quien llama debe
    /// detectar la ambiguedad en lugar de quedarse con la primera.
    /// </summary>
    Task<IReadOnlyList<Person>> GetAllByNameCaseInsensitiveAsync(string name);

    /// <summary>Indica si ya existe una persona con ese nombre y apellidos.</summary>
    Task<bool> ExistsWithNameAndLastNameAsync(string name, string? lastName, Guid? excludingId = null);

    /// <summary>Indica si el usuario ya está tomado por otra persona.</summary>
    Task<bool> ExistsWithUserNameAsync(string? userName, Guid? excludingId = null);
}