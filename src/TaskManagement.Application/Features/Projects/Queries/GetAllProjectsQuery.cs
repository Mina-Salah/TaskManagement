using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;

namespace TaskManagement.Application.Features.Projects.Queries;

public record GetAllProjectsQuery : IRequest<ApiResponse<IEnumerable<ProjectDto>>>;

public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, ApiResponse<IEnumerable<ProjectDto>>>
{
    private readonly IProjectApplicationService _projectService;

    public GetAllProjectsQueryHandler(IProjectApplicationService projectService) => _projectService = projectService;

    public async Task<ApiResponse<IEnumerable<ProjectDto>>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
        => await _projectService.GetAllAsync(request, cancellationToken);
}
