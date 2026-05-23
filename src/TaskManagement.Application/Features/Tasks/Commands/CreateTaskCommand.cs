using FluentValidation;
using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Tasks.Queries;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.Commands;

public record CreateTaskCommand(
    string Title,
    string Description,
    DateTime? DueDate,
    TaskPriority Priority,
    Guid ProjectId
) : IRequest<ApiResponse<TaskDto>>;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.DueDate).GreaterThan(DateTime.UtcNow).When(x => x.DueDate.HasValue)
            .WithMessage("DueDate must be in the future.");
    }
}

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, ApiResponse<TaskDto>>
{
    private readonly ITaskApplicationService _taskService;

    public CreateTaskCommandHandler(ITaskApplicationService taskService) => _taskService = taskService;

    public async Task<ApiResponse<TaskDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        => await _taskService.CreateAsync(request, cancellationToken);
}
