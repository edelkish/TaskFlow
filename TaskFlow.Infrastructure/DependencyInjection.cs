using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Mappings;
using TaskFlow.Application.Services;
using TaskFlow.Application.Validators;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Repositories;
using TaskFlow.Infrastructure.Services.Txt;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<TaskFlowDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Identity
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<TaskFlowDbContext>()
        .AddDefaultTokenProviders();

        // Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IPeriodRepository, PeriodRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<ITaskGroupRepository, TaskGroupRepository>();
        services.AddScoped<IPlanningTaskRepository, PlanningTaskRepository>();
        services.AddScoped<IImportBatchRepository, ImportBatchRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<ITaskGroupService, TaskGroupService>();
        services.AddScoped<IPlanningTaskService, PlanningTaskService>();
        services.AddScoped<IPeriodService, PeriodService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<ISettingsService, SettingsService>();

        // Parsing
        services.AddScoped<ITaskFileParser, TxtTaskParser>();

        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // Validators
        services.AddValidatorsFromAssembly(typeof(CreateProjectValidator).Assembly);

        return services;
    }
}