using AutoMapper;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // TaskCount y DevGroupName se resuelven en ProjectService.MapAsync: requieren
        // consultas aparte (backlog del proyecto y nombre del grupo).
        CreateMap<Project, ProjectDto>()
            .ForMember(dest => dest.TaskCount, opt => opt.Ignore())
            .ForMember(dest => dest.DevGroupName, opt => opt.Ignore());

        CreateMap<CreateProjectDto, Project>();
        CreateMap<UpdateProjectDto, Project>();

        // Los contadores no se mapean solos porque son colecciones y no propiedades
        // escalares; el repositorio las carga incluidas y aqui se mide su tamano.
        CreateMap<Period, PeriodDto>()
            .ForMember(d => d.GroupCount, o => o.MapFrom(s => s.TaskGroups.Count))
            .ForMember(d => d.ImportCount, o => o.MapFrom(s => s.ImportBatches.Count));

        // LastName y UserName se mapean por convención al tener el mismo nombre. FullName es
        // una propiedad calculada del record, se ignora para que no intente asignarla.
        CreateMap<Person, PersonDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore())
            .ForMember(dest => dest.RoleIds, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.Ignore());

        CreateMap<Role, RoleDto>();

        // El periodo puede ser null en un lote Failed (por ejemplo, rejections por
        // periodo inexistente), así que el nombre se resuelve con seguridad.
        CreateMap<ImportBatch, ImportBatchDto>()
            .ForMember(dest => dest.PeriodName, opt => opt.MapFrom(src => src.Period != null ? src.Period.Name : "(sin periodo)"))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<TaskGroup, TaskGroupDto>()
            .ForMember(dest => dest.PeriodName, opt => opt.MapFrom(src => src.Period.Name))
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project != null ? src.Project.Name : null))
            // Nombre y apellidos juntos: puede haber homonimos, asi que con el nombre solo
            // no se distingue a una persona de otra en la lista ni en el selector.
            .ForMember(dest => dest.DevName, opt => opt.MapFrom(src =>
                src.DevPerson != null ? PersonDisplayName.For(src.DevPerson.Name, src.DevPerson.LastName) : null))
            .ForMember(dest => dest.TeamLeadName, opt => opt.MapFrom(src =>
                src.TeamLeadPerson != null ? PersonDisplayName.For(src.TeamLeadPerson.Name, src.TeamLeadPerson.LastName) : null))
            .ForMember(dest => dest.QaName, opt => opt.MapFrom(src =>
                src.QaPerson != null ? PersonDisplayName.For(src.QaPerson.Name, src.QaPerson.LastName) : null));

        CreateMap<PlanningTask, PlanningTaskDto>()
            .ForMember(dest => dest.AssignedPersonName, opt => opt.MapFrom(src =>
                src.AssignedPerson != null ? PersonDisplayName.For(src.AssignedPerson.Name, src.AssignedPerson.LastName) : null));
    }
}