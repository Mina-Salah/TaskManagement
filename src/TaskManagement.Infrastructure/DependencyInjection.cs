using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;
using TaskManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
namespace TaskManagement.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
		services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
		services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Redis Cache (optional — falls back to no-op)
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            try
            {
                services.AddSingleton<IConnectionMultiplexer>(
                    ConnectionMultiplexer.Connect(redisConnection));
                services.AddScoped<ICacheService, RedisCacheService>();
            }
            catch
            {
                services.AddScoped<ICacheService, NoOpCacheService>();
            }
        }
        else
        {
            services.AddScoped<ICacheService, NoOpCacheService>();
        }

        return services;
    }
}
