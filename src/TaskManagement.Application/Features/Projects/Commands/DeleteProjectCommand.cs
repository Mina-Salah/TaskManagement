using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;

namespace TaskManagement.Application.Features.Projects.Commands;

public record DeleteProjectCommand(Guid Id) : IRequest<ApiResponse>;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, ApiResponse>
{
    private readonly IProjectApplicationService _projectService;

    public DeleteProjectCommandHandler(IProjectApplicationService projectService) => _projectService = projectService;

    public async Task<ApiResponse> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        => await _projectService.DeleteAsync(request, cancellationToken);
}
