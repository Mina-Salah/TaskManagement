using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Auth.Commands;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Common.Services;

public class AuthApplicationService : IAuthApplicationService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthApplicationService(IUnitOfWork uow, IPasswordService passwordService, ITokenService tokenService)
    {
        _uow = uow;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterCommand request, CancellationToken cancellationToken = default)
    {
        var users = _uow.Repository<User>();
        var email = request.Email.ToLower().Trim();

        if (await users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new ConflictException("Email is already registered.");

        var user = new User
        {
            FullName = request.FullName,
            Email = email,
            PasswordHash = _passwordService.HashPassword(request.Password)
        };

        await users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var token = _tokenService.GenerateToken(user);

        return ApiResponse<AuthResponseDto>.SuccessResult(
            new AuthResponseDto(user.Id, user.FullName, user.Email, token, user.Role.ToString()),
            "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.ToLower().Trim();
        var user = await _uow.Repository<User>().FirstOrDefaultAsync(user => user.Email == email, cancellationToken: cancellationToken)
            ?? throw new BadRequestException("Invalid email or password.");

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            throw new BadRequestException("Invalid email or password.");

        var token = _tokenService.GenerateToken(user);

        return ApiResponse<AuthResponseDto>.SuccessResult(
            new AuthResponseDto(user.Id, user.FullName, user.Email, token, user.Role.ToString()),
            "Login successful.");
    }
}
