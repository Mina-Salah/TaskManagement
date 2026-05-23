using AutoMapper;
using FluentAssertions;
using Moq;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Mappings;
using TaskManagement.Application.Common.Services;
using TaskManagement.Application.Features.Projects.Commands;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests.Features;

public class ProjectCommandsTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGenericRepository<Project>> _projectRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly IMapper _mapper;

    private readonly Guid _userId = Guid.NewGuid();

    public ProjectCommandsTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _uow.Setup(u => u.Repository<Project>()).Returns(_projectRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _cache.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateProject_ShouldReturnCreatedProject()
    {
        var command = new CreateProjectCommand("My Project", "Description here");
        _projectRepo.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Project p, CancellationToken _) => p);

        var service = new ProjectApplicationService(_uow.Object, _mapper, _currentUser.Object, _cache.Object);
        var handler = new CreateProjectCommandHandler(service);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("My Project");
    }

    [Fact]
    public async Task DeleteProject_ShouldThrowNotFound_WhenProjectDoesNotExist()
    {
        _projectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Project?)null);

        var service = new ProjectApplicationService(_uow.Object, _mapper, _currentUser.Object, _cache.Object);
        var handler = new DeleteProjectCommandHandler(service);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteProjectCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProject_ShouldThrowForbidden_WhenUserDoesNotOwnProject()
    {
        var project = new Project { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(project);

        var service = new ProjectApplicationService(_uow.Object, _mapper, _currentUser.Object, _cache.Object);
        var handler = new DeleteProjectCommandHandler(service);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProject_ShouldSucceed_WhenUserOwnsProject()
    {
        var project = new Project { Id = Guid.NewGuid(), UserId = _userId };
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(project);
        _projectRepo.Setup(r => r.DeleteAsync(project, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        var service = new ProjectApplicationService(_uow.Object, _mapper, _currentUser.Object, _cache.Object);
        var handler = new DeleteProjectCommandHandler(service);

        var result = await handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
    }
}
