using FluentValidation;
using MediatR;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Features.Auth.Commands;

// ─── Register ───────────────────────────────────────────────────────────────

public record RegisterCommand(string FullName, string Email, string Password) : IRequest<ApiResponse<AuthResponseDto>>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(IUnitOfWork uow, IPasswordService passwordService, ITokenService tokenService)
    {
        _uow = uow;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _uow.Users.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new ConflictException("Email is already registered.");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLower().Trim(),
            PasswordHash = _passwordService.HashPassword(request.Password)
        };

        await _uow.Users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var token = _tokenService.GenerateToken(user);

        return ApiResponse<AuthResponseDto>.SuccessResult(
            new AuthResponseDto(user.Id, user.FullName, user.Email, token, user.Role.ToString()),
            "Registration successful.");
    }
}

// ─── Login ───────────────────────────────────────────────────────────────────

public record LoginCommand(string Email, string Password) : IRequest<ApiResponse<AuthResponseDto>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(IUnitOfWork uow, IPasswordService passwordService, ITokenService tokenService)
    {
        _uow = uow;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email.ToLower().Trim(), cancellationToken)
            ?? throw new BadRequestException("Invalid email or password.");

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            throw new BadRequestException("Invalid email or password.");

        var token = _tokenService.GenerateToken(user);

        return ApiResponse<AuthResponseDto>.SuccessResult(
            new AuthResponseDto(user.Id, user.FullName, user.Email, token, user.Role.ToString()),
            "Login successful.");
    }
}

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record AuthResponseDto(Guid UserId, string FullName, string Email, string Token, string Role);
