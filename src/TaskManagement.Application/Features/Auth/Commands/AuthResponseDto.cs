namespace TaskManagement.Application.Features.Auth.Commands;

public record AuthResponseDto(Guid UserId, string FullName, string Email, string Token, string Role);
