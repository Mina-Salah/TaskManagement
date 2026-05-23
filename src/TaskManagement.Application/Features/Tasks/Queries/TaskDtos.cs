using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.Queries;

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    ProjectTaskStatus Status,
    DateTime? DueDate,
    TaskPriority Priority,
    Guid ProjectId,
    DateTime CreatedAt
);
