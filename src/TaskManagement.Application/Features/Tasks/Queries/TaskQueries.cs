using AutoMapper;
using MediatR;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Features.Tasks.Queries;

public record GetTasksByProjectQuery(Guid ProjectId) : IRequest<ApiResponse<IEnumerable<TaskDto>>>;

public class GetTasksByProjectQueryHandler : IRequestHandler<GetTasksByProjectQuery, ApiResponse<IEnumerable<TaskDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public GetTasksByProjectQueryHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse<IEnumerable<TaskDto>>> Handle(GetTasksByProjectQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await _uow.Projects.ExistsAsync(request.ProjectId, _currentUser.UserId, cancellationToken);
        if (!projectExists)
            throw new NotFoundException("Project", request.ProjectId);

        var tasks = await _uow.Tasks.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<TaskDto>>(tasks);

        return ApiResponse<IEnumerable<TaskDto>>.SuccessResult(dtos);
    }
}
