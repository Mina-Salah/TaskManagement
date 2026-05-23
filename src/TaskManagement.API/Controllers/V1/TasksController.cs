using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Features.Tasks.Commands;
using TaskManagement.Application.Features.Tasks.Queries;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers.V1;

[ApiController]
[ApiVersion(1)]
[Authorize]
[Route("api/v{version:apiVersion}")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get all tasks for a specific project</summary>
    [HttpGet("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProject(Guid projectId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTasksByProjectQuery(projectId), ct);
        return Ok(result);
    }

    /// <summary>Create a new task inside a project</summary>
    [HttpPost("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var command = new CreateTaskCommand(request.Title, request.Description, request.DueDate, request.Priority, projectId);
        var result = await _mediator.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Update a task (full update)</summary>
    [HttpPut("tasks/{taskId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid taskId, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var command = new UpdateTaskCommand(taskId, request.Title, request.Description, request.Status, request.DueDate, request.Priority);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Update only the status of a task</summary>
    [HttpPatch("tasks/{taskId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request, CancellationToken ct)
    {
        var command = new UpdateTaskStatusCommand(taskId, request.Status);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Delete a task</summary>
    [HttpDelete("tasks/{taskId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid taskId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteTaskCommand(taskId), ct);
        return Ok(result);
    }
}

public record CreateTaskRequest(
    string Title,
    string Description,
    DateTime? DueDate,
    TaskPriority Priority
);

public record UpdateTaskRequest(
    string Title,
    string Description,
    ProjectTaskStatus Status,
    DateTime? DueDate,
    TaskPriority Priority
);

public record UpdateTaskStatusRequest(ProjectTaskStatus Status);
