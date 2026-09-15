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
        var trimmed = name.Trim();
        return await _context.People
            .FirstOrDefaultAsync(p => p.Name.ToLower() == trimmed.ToLower());
    }
}