using AutoMapper;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Mappings;
using TaskManagement.Application.Common.Services;
using TaskManagement.Application.Features.Tasks.Commands;
using TaskManagement.Application.Features.Tasks.Queries;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests.Features;

public class TaskCommandsTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGenericRepository<Project>> _projectRepo = new();
    private readonly Mock<IGenericRepository<ProjectTask>> _taskRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly IMapper _mapper;

    private readonly Guid _userId = Guid.NewGuid();

    public TaskCommandsTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _uow.Setup(u => u.Repository<Project>()).Returns(_projectRepo.Object);
        _uow.Setup(u => u.Repository<ProjectTask>()).Returns(_taskRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _currentUser.Setup(c => c.UserId).Returns(_userId);
    }

    // ──────────────────────────────────────────────
    //  CreateTask
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTask_ShouldReturnCreatedTask_WhenProjectBelongsToUser()
    {
        var projectId = Guid.NewGuid();
        var command = new CreateTaskCommand("Fix bug", "Some description", null, TaskPriority.High, projectId);

        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _taskRepo.Setup(r => r.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask t, CancellationToken _) => t);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new CreateTaskCommandHandler(service);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Fix bug");
        result.Data.Priority.Should().Be(TaskPriority.High);
        result.Data.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task CreateTask_ShouldThrowNotFound_WhenProjectDoesNotBelongToUser()
    {
        var command = new CreateTaskCommand("Fix bug", "", null, TaskPriority.Low, Guid.NewGuid());

        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new CreateTaskCommandHandler(service);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    // ──────────────────────────────────────────────
    //  UpdateTask
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateTask_ShouldReturnUpdatedTask_WhenUserOwnsProject()
    {
        var projectId = Guid.NewGuid();
        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            Title = "Old title",
            Description = "Old desc",
            Status = ProjectTaskStatus.Todo,
            Priority = TaskPriority.Low,
            ProjectId = projectId
        };

        var command = new UpdateTaskCommand(task.Id, "New title", "New desc", ProjectTaskStatus.InProgress, null, TaskPriority.High);

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask t, CancellationToken _) => t);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new UpdateTaskCommandHandler(service);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("New title");
        result.Data.Status.Should().Be(ProjectTaskStatus.InProgress);
        result.Data.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        var command = new UpdateTaskCommand(Guid.NewGuid(), "Title", "", ProjectTaskStatus.Todo, null, TaskPriority.Low);

        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new UpdateTaskCommandHandler(service);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowForbidden_WhenUserDoesNotOwnProject()
    {
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid() };
        var command = new UpdateTaskCommand(task.Id, "Title", "", ProjectTaskStatus.Todo, null, TaskPriority.Low);

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new UpdateTaskCommandHandler(service);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    // ──────────────────────────────────────────────
    //  UpdateTaskStatus
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateTaskStatus_ShouldReturnUpdatedStatus_WhenUserOwnsProject()
    {
        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            Status = ProjectTaskStatus.Todo,
            ProjectId = Guid.NewGuid()
        };

        var command = new UpdateTaskStatusCommand(task.Id, ProjectTaskStatus.Done);

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask t, CancellationToken _) => t);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new UpdateTaskStatusCommandHandler(service);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(ProjectTaskStatus.Done);
    }

    [Fact]
    public async Task UpdateTaskStatus_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new UpdateTaskStatusCommandHandler(service);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateTaskStatusCommand(Guid.NewGuid(), ProjectTaskStatus.Done), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTaskStatus_ShouldThrowForbidden_WhenUserDoesNotOwnProject()
    {
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new UpdateTaskStatusCommandHandler(service);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new UpdateTaskStatusCommand(task.Id, ProjectTaskStatus.Done), CancellationToken.None));
    }

    // ──────────────────────────────────────────────
    //  DeleteTask
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteTask_ShouldSucceed_WhenUserOwnsProject()
    {
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _taskRepo.Setup(r => r.DeleteAsync(task, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new DeleteTaskCommandHandler(service);

        var result = await handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTask_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new DeleteTaskCommandHandler(service);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteTaskCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteTask_ShouldThrowForbidden_WhenUserDoesNotOwnProject()
    {
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new DeleteTaskCommandHandler(service);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None));
    }

    // ──────────────────────────────────────────────
    //  GetTasksByProject
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetTasksByProject_ShouldReturnTasks_WhenProjectBelongsToUser()
    {
        var projectId = Guid.NewGuid();
        var tasks = new List<ProjectTask>
        {
            new() { Id = Guid.NewGuid(), Title = "Task A", Priority = TaskPriority.High, ProjectId = projectId },
            new() { Id = Guid.NewGuid(), Title = "Task B", Priority = TaskPriority.Low,  ProjectId = projectId }
        };

        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _taskRepo.Setup(r => r.ListAsync(
                It.IsAny<Expression<Func<ProjectTask, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectTask>, IOrderedQueryable<ProjectTask>>?>(),
                It.IsAny<Func<IQueryable<ProjectTask>, IQueryable<ProjectTask>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new GetTasksByProjectQueryHandler(service);

        var result = await handler.Handle(new GetTasksByProjectQuery(projectId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTasksByProject_ShouldThrowNotFound_WhenProjectDoesNotBelongToUser()
    {
        _projectRepo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new TaskApplicationService(_uow.Object, _mapper, _currentUser.Object);
        var handler = new GetTasksByProjectQueryHandler(service);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetTasksByProjectQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
