using AutoMapper;
using FluentValidation;
using MediatR;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Tasks.Queries;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Features.Tasks.Commands;

// ─── Create Task ─────────────────────────────────────────────────────────────

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
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public CreateTaskCommandHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TaskDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await _uow.Projects.ExistsAsync(request.ProjectId, _currentUser.UserId, cancellationToken);
        if (!projectExists)
            throw new NotFoundException("Project", request.ProjectId);

        var task = _mapper.Map<ProjectTask>(request);

        await _uow.Tasks.AddAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<TaskDto>(task);
        return ApiResponse<TaskDto>.SuccessResult(dto, "Task created successfully.");
    }
}

// ─── Update Task Status ──────────────────────────────────────────────────────

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
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public UpdateTaskStatusCommandHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TaskDto>> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        var projectExists = await _uow.Projects.ExistsAsync(task.ProjectId, _currentUser.UserId, cancellationToken);
        if (!projectExists)
            throw new ForbiddenException();

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await _uow.Tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<TaskDto>(task);
        return ApiResponse<TaskDto>.SuccessResult(dto, "Task status updated.");
    }
}

// ─── Update Full Task ────────────────────────────────────────────────────────

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
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public UpdateTaskCommandHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TaskDto>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        var projectExists = await _uow.Projects.ExistsAsync(task.ProjectId, _currentUser.UserId, cancellationToken);
        if (!projectExists)
            throw new ForbiddenException();

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.DueDate = request.DueDate;
        task.Priority = request.Priority;
        task.UpdatedAt = DateTime.UtcNow;

        await _uow.Tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<TaskDto>(task);
        return ApiResponse<TaskDto>.SuccessResult(dto, "Task updated successfully.");
    }
}

// ─── Delete Task ─────────────────────────────────────────────────────────────

public record DeleteTaskCommand(Guid TaskId) : IRequest<ApiResponse>;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, ApiResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public DeleteTaskCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        var projectExists = await _uow.Projects.ExistsAsync(task.ProjectId, _currentUser.UserId, cancellationToken);
        if (!projectExists)
            throw new ForbiddenException();

        await _uow.Tasks.DeleteAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResult("Task deleted successfully.");
    }
}
