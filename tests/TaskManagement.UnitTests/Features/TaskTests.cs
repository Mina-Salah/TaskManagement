using AutoMapper;
using FluentAssertions;
using Moq;
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
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly IMapper _mapper;

    private readonly Guid _userId = Guid.NewGuid();

    public TaskCommandsTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _taskRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
    }

    private TaskApplicationService CreateService()
        => new(_taskRepo.Object, _projectRepo.Object, _mapper, _currentUser.Object);

    // ──────────────────────────────────────────────
    //  CreateTask
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTask_ShouldReturnCreatedTask_WhenProjectBelongsToUser()
    {
        var projectId = Guid.NewGuid();
        var command = new CreateTaskCommand("Fix bug", "Some description", null, TaskPriority.High, projectId);

        _projectRepo.Setup(r => r.BelongsToUserAsync(projectId, _userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
        _taskRepo.Setup(r => r.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var handler = new CreateTaskCommandHandler(CreateService());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("Fix bug");
        result.Data.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task CreateTask_ShouldThrowNotFound_WhenProjectDoesNotBelongToUser()
    {
        var command = new CreateTaskCommand("Fix bug", "", null, TaskPriority.Low, Guid.NewGuid());

        _projectRepo.Setup(r => r.BelongsToUserAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        var handler = new CreateTaskCommandHandler(CreateService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    // ──────────────────────────────────────────────
    //  UpdateTask
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateTask_ShouldReturnUpdatedTask_WhenUserOwnsProject()
    {
        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            Title = "Old title",
            ProjectId = Guid.NewGuid()
        };
        var command = new UpdateTaskCommand(task.Id, "New title", "New desc", ProjectTaskStatus.InProgress, null, TaskPriority.High);

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectRepo.Setup(r => r.BelongsToUserAsync(task.ProjectId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new UpdateTaskCommandHandler(CreateService());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Title.Should().Be("New title");
        result.Data.Status.Should().Be(ProjectTaskStatus.InProgress);
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((ProjectTask?)null);

        var handler = new UpdateTaskCommandHandler(CreateService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateTaskCommand(Guid.NewGuid(), "Title", "", ProjectTaskStatus.Todo, null, TaskPriority.Low), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTask_ShouldThrowForbidden_WhenUserDoesNotOwnProject()
    {
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectRepo.Setup(r => r.BelongsToUserAsync(task.ProjectId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new UpdateTaskCommandHandler(CreateService());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new UpdateTaskCommand(task.Id, "Title", "", ProjectTaskStatus.Todo, null, TaskPriority.Low), CancellationToken.None));
    }

    // ──────────────────────────────────────────────
    //  UpdateTaskStatus
    // ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateTaskStatus_ShouldReturnUpdatedStatus_WhenUserOwnsProject()
    {
        var task = new ProjectTask { Id = Guid.NewGuid(), Status = ProjectTaskStatus.Todo, ProjectId = Guid.NewGuid() };

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectRepo.Setup(r => r.BelongsToUserAsync(task.ProjectId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new UpdateTaskStatusCommandHandler(CreateService());
        var result = await handler.Handle(new UpdateTaskStatusCommand(task.Id, ProjectTaskStatus.Done), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(ProjectTaskStatus.Done);
    }

    [Fact]
    public async Task UpdateTaskStatus_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((ProjectTask?)null);

        var handler = new UpdateTaskStatusCommandHandler(CreateService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateTaskStatusCommand(Guid.NewGuid(), ProjectTaskStatus.Done), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTaskStatus_ShouldThrowForbidden_WhenUserDoesNotOwnProject()
    {
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectRepo.Setup(r => r.BelongsToUserAsync(task.ProjectId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new UpdateTaskStatusCommandHandler(CreateService());

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

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectRepo.Setup(r => r.BelongsToUserAsync(task.ProjectId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _taskRepo.Setup(r => r.DeleteAsync(task, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new DeleteTaskCommandHandler(CreateService());
        var result = await handler.Handle(new DeleteTaskCommand(task.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTask_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((ProjectTask?)null);

        var handler = new DeleteTaskCommandHandler(CreateService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteTaskCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteTask_ShouldThrowForbidden_WhenUserDoesNotOwnProject()
    {
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid() };

        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _projectRepo.Setup(r => r.BelongsToUserAsync(task.ProjectId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new DeleteTaskCommandHandler(CreateService());

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

        _projectRepo.Setup(r => r.BelongsToUserAsync(projectId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _taskRepo.Setup(r => r.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        var handler = new GetTasksByProjectQueryHandler(CreateService());
        var result = await handler.Handle(new GetTasksByProjectQuery(projectId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTasksByProject_ShouldThrowNotFound_WhenProjectDoesNotBelongToUser()
    {
        _projectRepo.Setup(r => r.BelongsToUserAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        var handler = new GetTasksByProjectQueryHandler(CreateService());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetTasksByProjectQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
