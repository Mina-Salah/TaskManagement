using FluentAssertions;
using Moq;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Features.Auth.Commands;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using Xunit;

namespace TaskManagement.UnitTests.Features;

public class AuthCommandsTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private readonly Mock<IUserRepository> _userRepo = new();

    public AuthCommandsTests()
    {
        _uow.Setup(u => u.Users).Returns(_userRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ─── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ShouldSucceed_WhenEmailIsUnique()
    {
        // Arrange
        var command = new RegisterCommand("Ahmed Ali", "ahmed@example.com", "Password123!");
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User u, CancellationToken _) => u);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_password");
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("jwt_token");

        var handler = new RegisterCommandHandler(_uow.Object, _passwordService.Object, _tokenService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("jwt_token");
        result.Data.Email.Should().Be("ahmed@example.com");
    }

    [Fact]
    public async Task Register_ShouldThrowConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new RegisterCommand("Ahmed Ali", "existing@example.com", "Password123!");
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var handler = new RegisterCommandHandler(_uow.Object, _passwordService.Object, _tokenService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ShouldSucceed_WithValidCredentials()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Ahmed Ali",
            Email = "ahmed@example.com",
            PasswordHash = "hashed_password"
        };

        var command = new LoginCommand("ahmed@example.com", "Password123!");
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordService.Setup(p => p.VerifyPassword("Password123!", "hashed_password")).Returns(true);
        _tokenService.Setup(t => t.GenerateToken(user)).Returns("jwt_token");

        var handler = new LoginCommandHandler(_uow.Object, _passwordService.Object, _tokenService.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Token.Should().Be("jwt_token");
    }

    [Fact]
    public async Task Login_ShouldThrowBadRequest_WhenUserNotFound()
    {
        // Arrange
        var command = new LoginCommand("MinaSalah@DotnetDeveloper.com", "Password123!");
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var handler = new LoginCommandHandler(_uow.Object, _passwordService.Object, _tokenService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_ShouldThrowBadRequest_WhenPasswordIsWrong()
    {
        // Arrange
        var user = new User { Email = "ahmed@example.com", PasswordHash = "hashed" };
        var command = new LoginCommand("ahmed@example.com", "WrongPassword");
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _passwordService.Setup(p => p.VerifyPassword("WrongPassword", "hashed")).Returns(false);

        var handler = new LoginCommandHandler(_uow.Object, _passwordService.Object, _tokenService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
