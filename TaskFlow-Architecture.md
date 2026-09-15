# TaskFlow - Arquitectura Limpia .NET 10

## Estructura de Solución

```
TaskFlow/
├── TaskFlow.Domain/                    # Capa de Dominio
│   ├── Entities/
│   │   ├── Project.cs
│   │   ├── Task.cs
│   │   └── User.cs (Identity)
│   ├── Enums/
│   │   ├── TaskStatus.cs
│   │   └── TaskPriority.cs
│   ├── Interfaces/
│   │   ├── IProjectRepository.cs
│   │   ├── ITaskRepository.cs
│   │   └── IUnitOfWork.cs
│   └── Exceptions/
│       ├── DomainException.cs
│       └── NotFoundException.cs
│
├── TaskFlow.Application/              # Capa de Aplicación
│   ├── DTOs/
│   │   ├── ProjectDto.cs
│   │   ├── TaskDto.cs
│   │   └── AuthDto.cs
│   ├── Interfaces/
│   │   ├── IProjectService.cs
│   │   ├── ITaskService.cs
│   │   └── IAuthService.cs
│   ├── Services/
│   │   ├── ProjectService.cs
│   │   ├── TaskService.cs
│   │   └── AuthService.cs
│   ├── Mappings/
│   │   └── MappingProfile.cs
│   ├── Validators/
│   │   ├── CreateProjectValidator.cs
│   │   └── CreateTaskValidator.cs
│   └── Common/
│       ├── Result.cs
│       └── RequestModels.cs
│
├── TaskFlow.Infrastructure/           # Capa de Infraestructura
│   ├── Data/
│   │   ├── TaskFlowDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── ProjectConfiguration.cs
│   │   │   └── TaskConfiguration.cs
│   │   └── Migrations/
│   ├── Identity/
│   │   ├── IdentityService.cs
│   │   └── JwtTokenService.cs
│   ├── Repositories/
│   │   ├── ProjectRepository.cs
│   │   ├── TaskRepository.cs
│   │   └── UnitOfWork.cs
│   └── DependencyInjection.cs
│
├── TaskFlow.Api/                       # API Web
│   ├── Controllers/
│   │   ├── ProjectsController.cs
│   │   ├── TasksController.cs
│   │   └── AuthController.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
│
└── TaskFlow.Blazor/                    # Frontend Blazor
    ├── Components/
    │   ├── Layout/
    │   ├── Pages/
    │   └── Shared/
    ├── Services/
    │   └── ApiClient.cs
    └── Program.cs
```

## Paquetes NuGet necesarios

### Domain
Ninguno (puro C#)

### Application
- FluentValidation
- AutoMapper
- MediatR (opcional, para CQRS)

### Infrastructure
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- Microsoft.IdentityModel.Tokens
- System.IdentityModel.Tokens.Jwt

### API
- Microsoft.AspNetCore.Authentication.JwtBearer
- Swashbuckle.AspNetCore (Swagger)

### Blazor
- Microsoft.AspNetCore.Components.Web

## Principios SOLID a aplicar

1. **S** - Cada servicio tiene una única responsabilidad
2. **O** - Abstracciones para extensibilidad (interfaces)
3. **L** - Repositorios intercambiables
4. **I** - Interfaces específicas (no grandes)
5. **D** - Dependency Injection en todas las capas

## Flujo de datos

```
Controller → Service → Repository → DbContext
    ↓           ↓
  DTO      Domain Entity
```
