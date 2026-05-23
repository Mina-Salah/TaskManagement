using FluentAssertions;
using Moq;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Common.Services;
using TaskManagement.Application.Features.Auth.Commands;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests.Features;

public class AuthCommandsTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private AuthApplicationService CreateService()
        => new(_userRepo.Object, _passwordService.Object, _tokenService.Object);

    [Fact]
    public async Task Register_ShouldSucceed_WhenEmailIsUnique()
    {
        var command = new RegisterCommand("Ahmed Ali", "ahmed@example.com", "Password123!");

        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_password");
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("jwt_token");

        var handler = new RegisterCommandHandler(CreateService());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().Be("jwt_token");
        result.Data.Email.Should().Be("ahmed@example.com");
    }

    [Fact]
    public async Task Register_ShouldThrowConflict_WhenEmailAlreadyExists()
    {
        var command = new RegisterCommand("Ahmed Ali", "existing@example.com", "Password123!");

        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var handler = new RegisterCommandHandler(CreateService());

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_ShouldSucceed_WithValidCredentials()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Ahmed Ali",
            Email = "ahmed@example.com",
            PasswordHash = "hashed_password"
        };

        var command = new LoginCommand("ahmed@example.com", "Password123!");

        _userRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordService.Setup(p => p.VerifyPassword("Password123!", "hashed_password")).Returns(true);
        _tokenService.Setup(t => t.GenerateToken(user)).Returns("jwt_token");

        var handler = new LoginCommandHandler(CreateService());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().Be("jwt_token");
    }

    [Fact]
    public async Task Login_ShouldThrowBadRequest_WhenUserNotFound()
    {
        var command = new LoginCommand("notfound@example.com", "Password123!");

        _userRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var handler = new LoginCommandHandler(CreateService());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_ShouldThrowBadRequest_WhenPasswordIsWrong()
    {
        var user = new User { Email = "ahmed@example.com", PasswordHash = "hashed" };
        var command = new LoginCommand("ahmed@example.com", "WrongPassword");

        _userRepo.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordService.Setup(p => p.VerifyPassword("WrongPassword", "hashed")).Returns(false);

        var handler = new LoginCommandHandler(CreateService());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
