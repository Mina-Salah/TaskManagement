using FluentValidation;
using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Tasks.Queries;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.Commands;

public record UpdateTaskStatusCommand(Guid TaskId, ProjectTaskStatus Status) : IRequest<ApiResponse<TaskDto>>;

public class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusCommand>
{
    public UpdateTaskStatusCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand, ApiResponse<TaskDto>>
{
    private readonly ITaskApplicationService _taskService;

    public UpdateTaskStatusCommandHandler(ITaskApplicationService taskService) => _taskService = taskService;

    public async Task<ApiResponse<TaskDto>> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
        => await _taskService.UpdateStatusAsync(request, cancellationToken);
}
