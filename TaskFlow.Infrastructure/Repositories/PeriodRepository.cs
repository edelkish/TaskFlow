using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class PeriodRepository : GenericRepository<Period>, IPeriodRepository
{
    public PeriodRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<Period?> GetByMonthYearAsync(int month, int year)
    {
        return await _context.Periods
            .FirstOrDefaultAsync(p => p.Month == month && p.Year == year);
    }

    public async Task<Period?> GetWithRelatedAsync(Guid id)
    {
        return await _context.Periods
            .Include(p => p.TaskGroups)
            .Include(p => p.ImportBatches)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Period>> GetOrderedDescWithRelatedAsync()
    {
        return await _context.Periods
            .Include(p => p.TaskGroups)
            .Include(p => p.ImportBatches)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync();
    }

    public Task<int> CountTaskGroupsAsync(Guid periodId)
    {
        return _context.TaskGroups.CountAsync(g => g.PeriodId == periodId);
    }

    public Task<int> CountImportBatchesAsync(Guid periodId)
    {
        return _context.ImportBatches.CountAsync(b => b.PeriodId == periodId);
    }
}

public class PersonRepository : GenericRepository<Person>, IPersonRepository
{
    public PersonRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Person>> GetAllByNameCaseInsensitiveAsync(string name)
    {
        // La columna People.Name usa la collation Latin1_General_CI_AI, así que la
        // comparación directa ya es insensible a mayúsculas y acentos, coincide con el
        // índice único y además es sargable. Usar ToLower() rompía esa concordancia:
        // "José" y "Jose" no empataban en la consulta pero sí colisionaban en el índice.
        //
        // Devuelve todas las coincidencias en lugar de la primera: desde que el nombre
        // único es (LastName, Name) puede haber varias, y elegir una a ciegas asignaria
        // el bloque del TXT a la persona equivocada.
        var trimmed = name.Trim();
        return await _context.People
            .Where(p => p.Name == trimmed)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<bool> ExistsWithNameAndLastNameAsync(string name, string? lastName, Guid? excludingId = null)
    {
        // La comparación de nombre hereda la collation CI_AI de la columna. Para el
        // apellido, null se traduce solo a "LastName IS NULL", que es exactamente lo
        // que distingue a dos homimos sin apellidos de los que si los tienen.
        //
        // excludingId permite comprobar el par durante una edición sin que la propia
        // persona se cuente a sí misma, igual que en ExistsWithUserNameAsync.
        var trimmedName = name.Trim();
        var trimmedLastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim();

        return await _context.People.AnyAsync(p =>
            p.Name == trimmedName && p.LastName == trimmedLastName
            && (excludingId == null || p.Id != excludingId));
    }

    public async Task<bool> ExistsWithUserNameAsync(string? userName, Guid? excludingId = null)
    {
        // Un usuario en blanco se guarda como NULL y no compite en el indice filtrado,
        // asi que se devuelve false directamente en lugar de consultar.
        if (string.IsNullOrWhiteSpace(userName))
            return false;

        var trimmed = userName.Trim();
        return await _context.People.AnyAsync(p =>
            p.UserName == trimmed && (excludingId == null || p.Id != excludingId));
    }
}