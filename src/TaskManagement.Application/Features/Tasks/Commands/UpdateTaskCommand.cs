using FluentValidation;
using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Tasks.Queries;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.Commands;

public record UpdateTaskCommand(
    Guid TaskId,
    string Title,
    string Description,
    ProjectTaskStatus Status,
    DateTime? DueDate,
    TaskPriority Priority
) : IRequest<ApiResponse<TaskDto>>;

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, ApiResponse<TaskDto>>
{
    private readonly ITaskApplicationService _taskService;

    public UpdateTaskCommandHandler(ITaskApplicationService taskService) => _taskService = taskService;

    public async Task<ApiResponse<TaskDto>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        => await _taskService.UpdateAsync(request, cancellationToken);
}
