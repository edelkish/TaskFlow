using TaskFlow.Domain.Entities;

namespace TaskFlow.Domain.Interfaces;

public interface IAppSettingRepository : IGenericRepository<AppSetting>
{
    Task<AppSetting?> GetByKeyAsync(string key);
}