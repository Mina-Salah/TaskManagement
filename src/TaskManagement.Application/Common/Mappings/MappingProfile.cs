using AutoMapper;
using TaskManagement.Application.Features.Auth.Commands;
using TaskManagement.Application.Features.Projects.Commands;
using TaskManagement.Application.Features.Projects.Queries;
using TaskManagement.Application.Features.Tasks.Commands;
using TaskManagement.Application.Features.Tasks.Queries;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<RegisterCommand, User>()
            .ForMember(d => d.PasswordHash, opt => opt.Ignore())
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.Role, opt => opt.Ignore())
            .ForMember(d => d.Projects, opt => opt.Ignore());

        // Project mappings
        CreateMap<CreateProjectCommand, Project>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.UserId, opt => opt.Ignore())
            .ForMember(d => d.User, opt => opt.Ignore())
            .ForMember(d => d.Tasks, opt => opt.Ignore());

        CreateMap<Project, ProjectDto>()
            .ForCtorParam(nameof(ProjectDto.TaskCount), opt => opt.MapFrom(s => s.Tasks.Count));

        CreateMap<Project, ProjectDetailDto>()
            .ForCtorParam(nameof(ProjectDetailDto.Tasks), opt => opt.MapFrom(s => s.Tasks));

        // Task mappings
        CreateMap<CreateTaskCommand, ProjectTask>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.Project, opt => opt.Ignore());

        CreateMap<ProjectTask, TaskDto>();
    }
}
