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

    public async Task<IEnumerable<Period>> GetOrderedDescAsync()
    {
        return await _context.Periods
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync();
    }
}

public class PersonRepository : GenericRepository<Person>, IPersonRepository
{
    public PersonRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<Person?> GetByNameCaseInsensitiveAsync(string name)
    {
        // La columna People.Name usa la collation Latin1_General_CI_AI, así que la
        // comparación directa ya es insensible a mayúsculas y acentos, coincide con el
        // índice único y además es sargable. Usar ToLower() rompía esa concordancia:
        // "José" y "Jose" no empataban en la consulta pero sí colisionaban en el índice.
        var trimmed = name.Trim();
        return await _context.People
            .FirstOrDefaultAsync(p => p.Name == trimmed);
    }
}