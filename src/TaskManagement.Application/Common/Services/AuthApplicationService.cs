using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Application.Features.Auth.Commands;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Common.Services;

public class AuthApplicationService : IAuthApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthApplicationService(IUserRepository userRepository, IPasswordService passwordService, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterCommand request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.ToLower().Trim();

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new ConflictException("Email is already registered.");

        var user = new User
        {
            FullName = request.FullName,
            Email = email,
            PasswordHash = _passwordService.HashPassword(request.Password)
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var token = _tokenService.GenerateToken(user);

        return ApiResponse<AuthResponseDto>.SuccessResult(
            new AuthResponseDto(user.Id, user.FullName, user.Email, token, user.Role.ToString()),
            "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.ToLower().Trim();

        var user = await _userRepository.FindByEmailAsync(email, cancellationToken)
            ?? throw new BadRequestException("Invalid email or password.");

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            throw new BadRequestException("Invalid email or password.");

        var token = _tokenService.GenerateToken(user);

        return ApiResponse<AuthResponseDto>.SuccessResult(
            new AuthResponseDto(user.Id, user.FullName, user.Email, token, user.Role.ToString()),
            "Login successful.");
    }
}
