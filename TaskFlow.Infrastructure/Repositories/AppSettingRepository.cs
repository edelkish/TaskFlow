using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class AppSettingRepository : GenericRepository<AppSetting>, IAppSettingRepository
{
    public AppSettingRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async Task<AppSetting?> GetByKeyAsync(string key)
    {
        return await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key);
    }
}