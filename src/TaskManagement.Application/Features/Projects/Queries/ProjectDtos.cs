using TaskManagement.Application.Features.Tasks.Queries;

namespace TaskManagement.Application.Features.Projects.Queries;

public record ProjectDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    int TaskCount
);

public record ProjectDetailDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    IEnumerable<TaskDto> Tasks
);
