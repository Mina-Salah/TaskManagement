using FluentValidation;
using MediatR;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Wrappers;

namespace TaskManagement.Application.Features.Auth.Commands;

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
    private readonly IAuthApplicationService _authService;

    public LoginCommandHandler(IAuthApplicationService authService) => _authService = authService;

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        => await _authService.LoginAsync(request, cancellationToken);
}
