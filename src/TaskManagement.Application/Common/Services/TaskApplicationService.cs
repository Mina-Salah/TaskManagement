using AutoMapper;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Tasks.Commands;
using TaskManagement.Application.Features.Tasks.Queries;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Common.Services;

public class TaskApplicationService : ITaskApplicationService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public TaskApplicationService(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TaskDto>> CreateAsync(CreateTaskCommand request, CancellationToken cancellationToken = default)
    {
        var projects = _uow.Repository<Project>();
        var tasks = _uow.Repository<ProjectTask>();

        var projectExists = await projects.AnyAsync(
            project => project.Id == request.ProjectId && project.UserId == _currentUser.UserId,
            cancellationToken);
        if (!projectExists)
            throw new NotFoundException("Project", request.ProjectId);

        var task = _mapper.Map<ProjectTask>(request);

        await tasks.AddAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<TaskDto>(task);
        return ApiResponse<TaskDto>.SuccessResult(dto, "Task created successfully.");
    }

    public async Task<ApiResponse<TaskDto>> UpdateAsync(UpdateTaskCommand request, CancellationToken cancellationToken = default)
    {
        var projects = _uow.Repository<Project>();
        var tasks = _uow.Repository<ProjectTask>();

        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        var projectExists = await projects.AnyAsync(
            project => project.Id == task.ProjectId && project.UserId == _currentUser.UserId,
            cancellationToken);
        if (!projectExists)
            throw new ForbiddenException();

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.DueDate = request.DueDate;
        task.Priority = request.Priority;
        task.UpdatedAt = DateTime.UtcNow;

        await tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<TaskDto>(task);
        return ApiResponse<TaskDto>.SuccessResult(dto, "Task updated successfully.");
    }

    public async Task<ApiResponse<TaskDto>> UpdateStatusAsync(UpdateTaskStatusCommand request, CancellationToken cancellationToken = default)
    {
        var projects = _uow.Repository<Project>();
        var tasks = _uow.Repository<ProjectTask>();

        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        var projectExists = await projects.AnyAsync(
            project => project.Id == task.ProjectId && project.UserId == _currentUser.UserId,
            cancellationToken);
        if (!projectExists)
            throw new ForbiddenException();

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<TaskDto>(task);
        return ApiResponse<TaskDto>.SuccessResult(dto, "Task status updated.");
    }

    public async Task<ApiResponse> DeleteAsync(DeleteTaskCommand request, CancellationToken cancellationToken = default)
    {
        var projects = _uow.Repository<Project>();
        var tasks = _uow.Repository<ProjectTask>();

        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        var projectExists = await projects.AnyAsync(
            project => project.Id == task.ProjectId && project.UserId == _currentUser.UserId,
            cancellationToken);
        if (!projectExists)
            throw new ForbiddenException();

        await tasks.DeleteAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResult("Task deleted successfully.");
    }

    public async Task<ApiResponse<IEnumerable<TaskDto>>> GetByProjectAsync(GetTasksByProjectQuery request, CancellationToken cancellationToken = default)
    {
        var projects = _uow.Repository<Project>();
        var tasksRepository = _uow.Repository<ProjectTask>();

        var projectExists = await projects.AnyAsync(
            project => project.Id == request.ProjectId && project.UserId == _currentUser.UserId,
            cancellationToken);
        if (!projectExists)
            throw new NotFoundException("Project", request.ProjectId);

        var tasks = await tasksRepository.ListAsync(
            task => task.ProjectId == request.ProjectId,
            query => query.OrderByDescending(task => task.Priority).ThenByDescending(task => task.CreatedAt),
            cancellationToken: cancellationToken);
        var dtos = _mapper.Map<IEnumerable<TaskDto>>(tasks);

        return ApiResponse<IEnumerable<TaskDto>>.SuccessResult(dtos);
    }
}
