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
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public TaskApplicationService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<TaskDto>> CreateAsync(CreateTaskCommand request, CancellationToken cancellationToken = default)
    {
        if (!await _projectRepository.BelongsToUserAsync(request.ProjectId, _currentUser.UserId, cancellationToken))
            throw new NotFoundException("Project", request.ProjectId);

        var task = _mapper.Map<ProjectTask>(request);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<TaskDto>.SuccessResult(_mapper.Map<TaskDto>(task), "Task created successfully.");
    }

    public async Task<ApiResponse<TaskDto>> UpdateAsync(UpdateTaskCommand request, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        if (!await _projectRepository.BelongsToUserAsync(task.ProjectId, _currentUser.UserId, cancellationToken))
            throw new ForbiddenException();

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.DueDate = request.DueDate;
        task.Priority = request.Priority;

        await _taskRepository.UpdateAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<TaskDto>.SuccessResult(_mapper.Map<TaskDto>(task), "Task updated successfully.");
    }

    public async Task<ApiResponse<TaskDto>> UpdateStatusAsync(UpdateTaskStatusCommand request, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        if (!await _projectRepository.BelongsToUserAsync(task.ProjectId, _currentUser.UserId, cancellationToken))
            throw new ForbiddenException();

        task.Status = request.Status;

        await _taskRepository.UpdateAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<TaskDto>.SuccessResult(_mapper.Map<TaskDto>(task), "Task status updated.");
    }

    public async Task<ApiResponse> DeleteAsync(DeleteTaskCommand request, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        if (!await _projectRepository.BelongsToUserAsync(task.ProjectId, _currentUser.UserId, cancellationToken))
            throw new ForbiddenException();

        await _taskRepository.DeleteAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse.SuccessResult("Task deleted successfully.");
    }

    public async Task<ApiResponse<IEnumerable<TaskDto>>> GetByProjectAsync(GetTasksByProjectQuery request, CancellationToken cancellationToken = default)
    {
        if (!await _projectRepository.BelongsToUserAsync(request.ProjectId, _currentUser.UserId, cancellationToken))
            throw new NotFoundException("Project", request.ProjectId);

        var tasks = await _taskRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

        return ApiResponse<IEnumerable<TaskDto>>.SuccessResult(_mapper.Map<IEnumerable<TaskDto>>(tasks));
    }
}
