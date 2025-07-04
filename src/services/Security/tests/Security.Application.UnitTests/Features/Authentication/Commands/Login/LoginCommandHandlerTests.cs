using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Security.Application.Features.Authentication.Commands.Login;
using Security.Application.UnitTests.Common;
using Security.Domain.Entities;

namespace Security.Application.UnitTests.Features.Authentication.Commands.Login;

[TestFixture]
public class LoginCommandHandlerTests : CommandHandlerTestBase
{
    private LoginCommandHandler _handler = null!;
    private Mock<ILogger<LoginCommandHandler>> _mockLogger = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        
        _mockLogger = CreateMockLogger<LoginCommandHandler>();
        _handler = new LoginCommandHandler(
            MockUserManager.Object,
            MockTokenService.Object,
            MockRefreshTokenService.Object,
            MockAuditService.Object,
            _mockLogger.Object,
            CreateSecurityOptions());
    }

    [Test]
    public async Task Handle_ValidCredentials_ShouldReturnSuccessWithTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand(
            user.UserName!,
            "ValidPassword123!",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var accessToken = CreateValidJwtToken();
        var jwtId = Guid.NewGuid().ToString();
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);
        var refreshToken = CreateTestRefreshToken(user.Id, jwtId: jwtId);

        SetupUserManagerFindByName(user);
        SetupUserManagerCheckPassword(true);
        SetupUserManagerUpdateUser(IdentityResult.Success);
        SetupUserManagerGetRoles(new List<string> { "User" });
        SetupTokenServiceCreateAccessToken(accessToken, jwtId, accessExpiry);
        SetupRefreshTokenServiceCreate(refreshToken);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        if (result.IsFailure)
        {
            Console.WriteLine($"Login failed with error: {result.Error}");
        }
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be(accessToken);
        result.Value.RefreshToken.Should().Be(refreshToken.Token);
        result.Value.AccessTokenExpiry.Should().Be(accessExpiry);
        result.Value.RefreshTokenExpiry.Should().Be(refreshToken.ExpiryDate);

        // Verify logging
        MockAuditService.Verify(
            x => x.LogSuccessfulAuthenticationAsync(user.Id, command.IpAddress),
            Times.Once);

        // Verify user update (reset failed attempts)
        MockUserManager.Verify(
            x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.FailedLoginAttempts == 0)),
            Times.Once);
    }

    [Test]
    public async Task Handle_UserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginCommand(
            "nonexistent@example.com",
            "Password123!",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(null);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid username or password");

        // Verify audit logging
        MockAuditService.Verify(
            x => x.LogFailedAuthenticationAsync(command.UserName, command.IpAddress, "User not found"),
            Times.Once);
    }

    [Test]
    public async Task Handle_InactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var command = new LoginCommand(
            user.UserName!,
            "Password123!",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(user);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account is deactivated");

        // Verify audit logging
        MockAuditService.Verify(
            x => x.LogFailedAuthenticationAsync(user.Id, command.IpAddress, "Account deactivated"),
            Times.Once);
    }

    [Test]
    public async Task Handle_LockedOutUser_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser(failedLoginAttempts: 6);
        user.UpdatedAt = DateTime.UtcNow.AddMinutes(-5); // Within lockout period
        
        var command = new LoginCommand(
            user.UserName!,
            "Password123!",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(user);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account is temporarily locked due to multiple failed attempts");

        // Verify audit logging
        MockAuditService.Verify(
            x => x.LogFailedAuthenticationAsync(user.Id, command.IpAddress, "Account locked out"),
            Times.Once);
    }

    [Test]
    public async Task Handle_InvalidPassword_ShouldReturnFailureAndIncrementFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser(failedLoginAttempts: 2);
        var command = new LoginCommand(
            user.UserName!,
            "WrongPassword",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(user);
        SetupUserManagerCheckPassword(false);
        SetupUserManagerUpdateUser(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid username or password");

        // Verify failed attempts were incremented
        MockUserManager.Verify(
            x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.FailedLoginAttempts == 3)),
            Times.Once);

        // Verify audit logging
        MockAuditService.Verify(
            x => x.LogFailedAuthenticationAsync(user.Id, command.IpAddress, "Invalid password"),
            Times.Once);
    }

    [Test]
    public async Task Handle_RefreshTokenCreationFails_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand(
            user.UserName!,
            "ValidPassword123!",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var accessToken = CreateValidJwtToken();
        var jwtId = Guid.NewGuid().ToString();
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);

        SetupUserManagerFindByName(user);
        SetupUserManagerCheckPassword(true);
        SetupUserManagerUpdateUser(IdentityResult.Success);
        SetupUserManagerGetRoles(new List<string> { "User" });
        SetupTokenServiceCreateAccessToken(accessToken, jwtId, accessExpiry);
        SetupRefreshTokenServiceCreate(null); // Refresh token creation fails

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Authentication failed");
    }

    [Test]
    public async Task Handle_ValidCommand_ShouldGenerateTokenWithCorrectClaims()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand(
            user.UserName!,
            "ValidPassword123!",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var accessToken = CreateValidJwtToken();
        var jwtId = Guid.NewGuid().ToString();
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);
        var refreshToken = CreateTestRefreshToken(user.Id, jwtId: jwtId);

        SetupUserManagerFindByName(user);
        SetupUserManagerCheckPassword(true);
        SetupUserManagerUpdateUser(IdentityResult.Success);
        SetupTokenServiceCreateAccessToken(accessToken, jwtId, accessExpiry);
        SetupRefreshTokenServiceCreate(refreshToken);

        MockUserManager
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify token service was called with user and claims
        MockTokenService.Verify(
            x => x.CreateAccessTokenAsync(
                It.Is<ApplicationUser>(u => u.Id == user.Id),
                It.IsAny<IEnumerable<System.Security.Claims.Claim>>()),
            Times.Once);
    }

    [Test]
    public void Handle_NullCommand_ShouldThrowArgumentNullException()
    {
        // Arrange
        LoginCommand command = null!;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.Handle(command, CreateCancellationToken()));
    }

    [Test]
    public async Task Handle_ExceptionDuringProcessing_ShouldReturnFailureAndLogError()
    {
        // Arrange
        var command = new LoginCommand(
            "testuser@example.com",
            "Password123!",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        MockUserManager
            .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("An error occurred during authentication");
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public async Task Handle_InvalidUserName_ShouldReturnFailure(string? userName)
    {
        // Arrange
        var command = new LoginCommand(
            userName!,
            "Password123!",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(null);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid username or password");
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public async Task Handle_InvalidPassword_ShouldReturnFailure(string? password)
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand(
            user.UserName!,
            password!,
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(user);
        SetupUserManagerCheckPassword(false);
        SetupUserManagerUpdateUser(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid username or password");
    }
}
