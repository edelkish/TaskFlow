using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface IPeriodRepository : IGenericRepository<Period>
{
    Task<Period?> GetByMonthYearAsync(int month, int year);
    Task<IEnumerable<Period>> GetOrderedDescAsync();
}

public interface IPersonRepository : IGenericRepository<Person>
{
    Task<Person?> GetByNameCaseInsensitiveAsync(string name);
}