using AutoMapper;
using FluentValidation;
using MediatR;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Projects.Queries;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Features.Projects.Commands;

// ─── Create Project ──────────────────────────────────────────────────────────

public record CreateProjectCommand(string Name, string Description) : IRequest<ApiResponse<ProjectDto>>;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ApiResponse<ProjectDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public CreateProjectCommandHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser, ICacheService cache)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<ApiResponse<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = _mapper.Map<Project>(request);
        project.UserId = _currentUser.UserId;

        await _uow.Projects.AddAsync(project, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPrefixAsync($"projects:user:{_currentUser.UserId}", cancellationToken);

        var dto = _mapper.Map<ProjectDto>(project);
        return ApiResponse<ProjectDto>.SuccessResult(dto, "Project created successfully.");
    }
}

// ─── Update Project ──────────────────────────────────────────────────────────

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
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public UpdateProjectCommandHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser, ICacheService cache)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<ApiResponse<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _uow.Projects.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Project", request.Id);

        if (project.UserId != _currentUser.UserId)
            throw new ForbiddenException();

        project.Name = request.Name;
        project.Description = request.Description;
        project.UpdatedAt = DateTime.UtcNow;

        await _uow.Projects.UpdateAsync(project, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPrefixAsync($"projects:user:{_currentUser.UserId}", cancellationToken);

        var dto = _mapper.Map<ProjectDto>(project);
        return ApiResponse<ProjectDto>.SuccessResult(dto, "Project updated successfully.");
    }
}

// ─── Delete Project ──────────────────────────────────────────────────────────

public record DeleteProjectCommand(Guid Id) : IRequest<ApiResponse>;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, ApiResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public DeleteProjectCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, ICacheService cache)
    {
        _uow = uow;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<ApiResponse> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _uow.Projects.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Project", request.Id);

        if (project.UserId != _currentUser.UserId)
            throw new ForbiddenException();

        await _uow.Projects.DeleteAsync(project, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPrefixAsync($"projects:user:{_currentUser.UserId}", cancellationToken);

        return ApiResponse.SuccessResult("Project deleted successfully.");
    }
}
