using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;

namespace TaskManagement.Application.Features.Tasks.Queries;

public record GetTasksByProjectQuery(Guid ProjectId) : IRequest<ApiResponse<IEnumerable<TaskDto>>>;

public class GetTasksByProjectQueryHandler : IRequestHandler<GetTasksByProjectQuery, ApiResponse<IEnumerable<TaskDto>>>
{
    private readonly ITaskApplicationService _taskService;

    public GetTasksByProjectQueryHandler(ITaskApplicationService taskService) => _taskService = taskService;

    public async Task<ApiResponse<IEnumerable<TaskDto>>> Handle(GetTasksByProjectQuery request, CancellationToken cancellationToken)
        => await _taskService.GetByProjectAsync(request, cancellationToken);
}
