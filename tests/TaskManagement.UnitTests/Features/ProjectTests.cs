using AutoMapper;
using FluentAssertions;
using Moq;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Mappings;
using TaskManagement.Application.Features.Projects.Commands;
using TaskManagement.Application.Features.Projects.Queries;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests.Features;

public class ProjectCommandsTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly IMapper _mapper;

    private readonly Guid _userId = Guid.NewGuid();

    public ProjectCommandsTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _uow.Setup(u => u.Projects).Returns(_projectRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _cache.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateProject_ShouldReturnCreatedProject()
    {
        // Arrange
        var command = new CreateProjectCommand("My Project", "Description here");
        _projectRepo.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Project p, CancellationToken _) => p);

        var handler = new CreateProjectCommandHandler(_uow.Object, _mapper, _currentUser.Object, _cache.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("My Project");
    }

    [Fact]
    public async Task DeleteProject_ShouldThrowNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        _projectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Project?)null);

        var handler = new DeleteProjectCommandHandler(_uow.Object, _currentUser.Object, _cache.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteProjectCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProject_ShouldThrowForbidden_WhenUserDoesNotOwnProject()
    {
        // Arrange
        var project = new Project { Id = Guid.NewGuid(), UserId = Guid.NewGuid() }; // different user
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(project);

        var handler = new DeleteProjectCommandHandler(_uow.Object, _currentUser.Object, _cache.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProject_ShouldSucceed_WhenUserOwnsProject()
    {
        // Arrange
        var project = new Project { Id = Guid.NewGuid(), UserId = _userId };
        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(project);
        _projectRepo.Setup(r => r.DeleteAsync(project, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

        var handler = new DeleteProjectCommandHandler(_uow.Object, _currentUser.Object, _cache.Object);

        // Act
        var result = await handler.Handle(new DeleteProjectCommand(project.Id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }
}
