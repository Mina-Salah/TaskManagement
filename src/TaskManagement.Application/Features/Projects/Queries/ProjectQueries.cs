using AutoMapper;
using MediatR;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Features.Projects.Queries;

// ─── Get All Projects ────────────────────────────────────────────────────────

public record GetAllProjectsQuery : IRequest<ApiResponse<IEnumerable<ProjectDto>>>;

public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, ApiResponse<IEnumerable<ProjectDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public GetAllProjectsQueryHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser, ICacheService cache)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<ApiResponse<IEnumerable<ProjectDto>>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"projects:user:{_currentUser.UserId}";
        var cached = await _cache.GetAsync<IEnumerable<ProjectDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return ApiResponse<IEnumerable<ProjectDto>>.SuccessResult(cached);

        var projects = await _uow.Projects.GetByUserIdAsync(_currentUser.UserId, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<ProjectDto>>(projects);

        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5), cancellationToken);

        return ApiResponse<IEnumerable<ProjectDto>>.SuccessResult(dtos);
    }
}

// ─── Get Project By Id ───────────────────────────────────────────────────────

public record GetProjectByIdQuery(Guid Id) : IRequest<ApiResponse<ProjectDetailDto>>;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ApiResponse<ProjectDetailDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public GetProjectByIdQueryHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<ProjectDetailDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _uow.Projects.GetByIdWithTasksAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Project", request.Id);

        if (project.UserId != _currentUser.UserId)
            throw new ForbiddenException();

        var dto = _mapper.Map<ProjectDetailDto>(project);
        return ApiResponse<ProjectDetailDto>.SuccessResult(dto);
    }
}
