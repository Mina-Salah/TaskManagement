using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Auth.Commands;
using TaskManagement.Application.Features.Projects.Commands;
using TaskManagement.Application.Features.Projects.Queries;
using TaskManagement.Application.Features.Tasks.Commands;
using TaskManagement.Application.Features.Tasks.Queries;

namespace TaskManagement.Application.Common.Interfaces;

public interface IAuthApplicationService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterCommand request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginCommand request, CancellationToken cancellationToken = default);
}

public interface IProjectApplicationService
{
    Task<ApiResponse<ProjectDto>> CreateAsync(CreateProjectCommand request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectDto>> UpdateAsync(UpdateProjectCommand request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteProjectCommand request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<ProjectDto>>> GetAllAsync(GetAllProjectsQuery request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProjectDetailDto>> GetByIdAsync(GetProjectByIdQuery request, CancellationToken cancellationToken = default);
}

public interface ITaskApplicationService
{
    Task<ApiResponse<TaskDto>> CreateAsync(CreateTaskCommand request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TaskDto>> UpdateAsync(UpdateTaskCommand request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TaskDto>> UpdateStatusAsync(UpdateTaskStatusCommand request, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(DeleteTaskCommand request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<TaskDto>>> GetByProjectAsync(GetTasksByProjectQuery request, CancellationToken cancellationToken = default);
}
