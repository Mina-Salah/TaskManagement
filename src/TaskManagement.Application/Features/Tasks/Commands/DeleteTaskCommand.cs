using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;

namespace TaskManagement.Application.Features.Tasks.Commands;

public record DeleteTaskCommand(Guid TaskId) : IRequest<ApiResponse>;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, ApiResponse>
{
    private readonly ITaskApplicationService _taskService;

    public DeleteTaskCommandHandler(ITaskApplicationService taskService) => _taskService = taskService;

    public async Task<ApiResponse> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        => await _taskService.DeleteAsync(request, cancellationToken);
}
