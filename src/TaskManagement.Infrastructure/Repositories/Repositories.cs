using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

// ─── User Repository ──────────────────────────────────────────────────────────

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Users.FindAsync(new object[] { id }, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        return user;
    }

    public Task<User> UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        return Task.FromResult(user);
    }
}

// ─── Project Repository ───────────────────────────────────────────────────────

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context) => _context = context;

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Projects.FindAsync(new object[] { id }, ct);

    public async Task<Project?> GetByIdWithTasksAsync(Guid id, CancellationToken ct = default)
        => await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IEnumerable<Project>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.Projects
            .Include(p => p.Tasks)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid id, Guid userId, CancellationToken ct = default)
        => await _context.Projects.AnyAsync(p => p.Id == id && p.UserId == userId, ct);

    public async Task<Project> AddAsync(Project project, CancellationToken ct = default)
    {
        await _context.Projects.AddAsync(project, ct);
        return project;
    }

    public Task<Project> UpdateAsync(Project project, CancellationToken ct = default)
    {
        _context.Projects.Update(project);
        return Task.FromResult(project);
    }

    public Task DeleteAsync(Project project, CancellationToken ct = default)
    {
        _context.Projects.Remove(project);
        return Task.CompletedTask;
    }
}

// ─── Task Repository ──────────────────────────────────────────────────────────

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context) => _context = context;

    public async Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Tasks.FindAsync(new object[] { id }, ct);

    public async Task<IEnumerable<ProjectTask>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
        => await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<ProjectTask> AddAsync(ProjectTask task, CancellationToken ct = default)
    {
        await _context.Tasks.AddAsync(task, ct);
        return task;
    }

    public Task<ProjectTask> UpdateAsync(ProjectTask task, CancellationToken ct = default)
    {
        _context.Tasks.Update(task);
        return Task.FromResult(task);
    }

    public Task DeleteAsync(ProjectTask task, CancellationToken ct = default)
    {
        _context.Tasks.Remove(task);
        return Task.CompletedTask;
    }
}

// ─── Unit of Work ─────────────────────────────────────────────────────────────

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IUserRepository Users { get; }
    public IProjectRepository Projects { get; }
    public ITaskRepository Tasks { get; }

    public UnitOfWork(AppDbContext context,
        IUserRepository users,
        IProjectRepository projects,
        ITaskRepository tasks)
    {
        _context = context;
        Users = users;
        Projects = projects;
        Tasks = tasks;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
