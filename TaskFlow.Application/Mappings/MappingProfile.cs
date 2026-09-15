using AutoMapper;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Project, ProjectDto>()
            .ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.Tasks.Count));

        CreateMap<CreateProjectDto, Project>();
        CreateMap<UpdateProjectDto, Project>();

        CreateMap<TaskItem, TaskDto>()
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project.Name));

        CreateMap<CreateTaskDto, TaskItem>();
        CreateMap<UpdateTaskDto, TaskItem>();

        CreateMap<Period, PeriodDto>();

        CreateMap<Person, PersonDto>();

        CreateMap<ImportBatch, ImportBatchDto>()
            .ForMember(dest => dest.PeriodName, opt => opt.MapFrom(src => src.Period.Name))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<TaskGroup, TaskGroupDto>()
            .ForMember(dest => dest.PeriodName, opt => opt.MapFrom(src => src.Period.Name))
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project != null ? src.Project.Name : null))
            .ForMember(dest => dest.DevName, opt => opt.MapFrom(src => src.DevPerson != null ? src.DevPerson.Name : null))
            .ForMember(dest => dest.TeamLeadName, opt => opt.MapFrom(src => src.TeamLeadPerson != null ? src.TeamLeadPerson.Name : null))
            .ForMember(dest => dest.QaName, opt => opt.MapFrom(src => src.QaPerson != null ? src.QaPerson.Name : null));

        CreateMap<PlanningTask, PlanningTaskDto>()
            .ForMember(dest => dest.AssignedPersonName, opt => opt.MapFrom(src => src.AssignedPerson != null ? src.AssignedPerson.Name : null));
    }
}