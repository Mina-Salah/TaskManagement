using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;

namespace TaskManagement.Application.Features.Projects.Queries;

public record GetProjectByIdQuery(Guid Id) : IRequest<ApiResponse<ProjectDetailDto>>;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ApiResponse<ProjectDetailDto>>
{
    private readonly IProjectApplicationService _projectService;

    public GetProjectByIdQueryHandler(IProjectApplicationService projectService) => _projectService = projectService;

    public async Task<ApiResponse<ProjectDetailDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
        => await _projectService.GetByIdAsync(request, cancellationToken);
}
