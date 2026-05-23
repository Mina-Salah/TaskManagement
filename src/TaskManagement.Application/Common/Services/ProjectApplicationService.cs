using AutoMapper;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Projects.Commands;
using TaskManagement.Application.Features.Projects.Queries;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Common.Services;

public class ProjectApplicationService : IProjectApplicationService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public ProjectApplicationService(
        IProjectRepository projectRepository,
        IMapper mapper,
        ICurrentUserService currentUser,
        ICacheService cache)
    {
        _projectRepository = projectRepository;
        _mapper = mapper;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<ApiResponse<ProjectDto>> CreateAsync(CreateProjectCommand request, CancellationToken cancellationToken = default)
    {
        var project = _mapper.Map<Project>(request);
        project.UserId = _currentUser.UserId;

        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPrefixAsync($"projects:user:{_currentUser.UserId}", cancellationToken);

        var dto = _mapper.Map<ProjectDto>(project);
        return ApiResponse<ProjectDto>.SuccessResult(dto, "Project created successfully.");
    }

    public async Task<ApiResponse<ProjectDto>> UpdateAsync(UpdateProjectCommand request, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Project", request.Id);

        if (project.UserId != _currentUser.UserId)
            throw new ForbiddenException();

        project.Name = request.Name;
        project.Description = request.Description;

        await _projectRepository.UpdateAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPrefixAsync($"projects:user:{_currentUser.UserId}", cancellationToken);

        var dto = _mapper.Map<ProjectDto>(project);
        return ApiResponse<ProjectDto>.SuccessResult(dto, "Project updated successfully.");
    }

    public async Task<ApiResponse> DeleteAsync(DeleteProjectCommand request, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Project", request.Id);

        if (project.UserId != _currentUser.UserId)
            throw new ForbiddenException();

        await _projectRepository.DeleteAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPrefixAsync($"projects:user:{_currentUser.UserId}", cancellationToken);

        return ApiResponse.SuccessResult("Project deleted successfully.");
    }

    public async Task<ApiResponse<IEnumerable<ProjectDto>>> GetAllAsync(GetAllProjectsQuery request, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"projects:user:{_currentUser.UserId}";
        var cached = await _cache.GetAsync<IEnumerable<ProjectDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return ApiResponse<IEnumerable<ProjectDto>>.SuccessResult(cached);

        var projects = await _projectRepository.GetAllByUserWithTasksAsync(_currentUser.UserId, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<ProjectDto>>(projects);

        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5), cancellationToken);

        return ApiResponse<IEnumerable<ProjectDto>>.SuccessResult(dtos);
    }

    public async Task<ApiResponse<ProjectDetailDto>> GetByIdAsync(GetProjectByIdQuery request, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdWithTasksAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Project", request.Id);

        if (project.UserId != _currentUser.UserId)
            throw new ForbiddenException();

        var dto = _mapper.Map<ProjectDetailDto>(project);
        return ApiResponse<ProjectDetailDto>.SuccessResult(dto);
    }
}
