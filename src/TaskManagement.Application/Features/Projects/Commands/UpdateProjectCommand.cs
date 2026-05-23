using FluentValidation;
using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Projects.Queries;

namespace TaskManagement.Application.Features.Projects.Commands;

public record UpdateProjectCommand(Guid Id, string Name, string Description) : IRequest<ApiResponse<ProjectDto>>;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ApiResponse<ProjectDto>>
{
    private readonly IProjectApplicationService _projectService;

    public UpdateProjectCommandHandler(IProjectApplicationService projectService) => _projectService = projectService;

    public async Task<ApiResponse<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        => await _projectService.UpdateAsync(request, cancellationToken);
}
