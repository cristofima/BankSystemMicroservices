using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using Security.Application.Features.Authentication.Commands.RefreshToken;
using Security.Application.UnitTests.Common;
using Security.Domain.Entities;
using RefreshTokenEntity = Security.Domain.Entities.RefreshToken;

namespace Security.Application.UnitTests.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandlerTests : CommandHandlerTestBase
{
    private RefreshTokenCommandHandler _handler = null!;
    private Mock<ILogger<RefreshTokenCommandHandler>> _mockLogger = null!;

    public RefreshTokenCommandHandlerTests()
    {
        _mockLogger = CreateMockLogger<RefreshTokenCommandHandler>();
        _handler = new RefreshTokenCommandHandler(
            MockUserManager.Object,
            MockTokenService.Object,
            MockRefreshTokenService.Object,
            MockAuditService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var jwtId = Guid.NewGuid().ToString();
        var refreshToken = CreateTestRefreshToken(user.Id, jwtId: jwtId);
        var accessToken = CreateValidJwtToken();
        
        var command = new RefreshTokenCommand(
            accessToken,
            refreshToken.Token,
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var newAccessToken = CreateValidJwtToken();
        var newJwtId = Guid.NewGuid().ToString();
        var newAccessExpiry = DateTime.UtcNow.AddMinutes(15);
        var newRefreshToken = CreateTestRefreshToken(user.Id, jwtId: newJwtId);

        // Setup mocks
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("jti", jwtId)
        }));

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns(claimsPrincipal);

        MockUserManager
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        MockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        MockRefreshTokenService
            .Setup(x => x.ValidateRefreshTokenAsync(
                refreshToken.Token,
                jwtId,
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        MockRefreshTokenService
            .Setup(x => x.RefreshTokenAsync(
                refreshToken,
                It.IsAny<string>(),
                command.IpAddress,
                command.DeviceInfo,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRefreshToken);

        SetupTokenServiceCreateAccessToken(newAccessToken, newJwtId, newAccessExpiry);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be(newAccessToken);
        result.Value.RefreshToken.Should().Be(newRefreshToken.Token);
        result.Value.AccessTokenExpiry.Should().Be(newAccessExpiry);
        result.Value.RefreshTokenExpiry.Should().Be(newRefreshToken.ExpiryDate);

        // Verify audit logging
        MockAuditService.Verify(
            x => x.LogTokenRefreshAsync(user.Id, command.IpAddress),
            Times.Once);
    }

    [Theory]
    [InlineData("invalid-access-token-1")]
    [InlineData("invalid-access-token-2")]
    public async Task Handle_InvalidAccessToken_ShouldReturnFailure(string invalidAccessToken)
    {
        // Arrange
        var command = new RefreshTokenCommand(
            invalidAccessToken,
            "valid-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ShouldReturnFailure_TokenExpired()
    {
        // Arrange
        var command = new RefreshTokenCommand("expired-access-token", "valid-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid token");
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ShouldReturnFailure_TokenMalformed()
    {
        // Arrange
        var command = new RefreshTokenCommand("malformed-access-token", "valid-refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid token");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var jwtId = Guid.NewGuid().ToString();
        var accessToken = CreateValidJwtToken();
        var refreshTokenValue = CreateValidRefreshToken();
        
        var command = new RefreshTokenCommand(
            accessToken,
            refreshTokenValue,
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("jti", jwtId)
        }));

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns(claimsPrincipal);

        MockUserManager
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task Handle_InactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var jwtId = Guid.NewGuid().ToString();
        var accessToken = CreateValidJwtToken();
        var refreshTokenValue = CreateValidRefreshToken();
        
        var command = new RefreshTokenCommand(
            accessToken,
            refreshTokenValue,
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("jti", jwtId)
        }));

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns(claimsPrincipal);

        MockUserManager
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task Handle_InvalidRefreshToken_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var jwtId = Guid.NewGuid().ToString();
        var accessToken = CreateValidJwtToken();
        var refreshTokenValue = CreateValidRefreshToken();
        
        var command = new RefreshTokenCommand(
            accessToken,
            refreshTokenValue,
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("jti", jwtId)
        }));

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns(claimsPrincipal);

        MockUserManager
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        MockRefreshTokenService
            .Setup(x => x.ValidateRefreshTokenAsync(
                refreshTokenValue,
                jwtId,
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenEntity?)null);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task Handle_RefreshTokenCreationFails_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        var jwtId = Guid.NewGuid().ToString();
        var refreshToken = CreateTestRefreshToken(user.Id, jwtId: jwtId);
        var accessToken = CreateValidJwtToken();
        
        var command = new RefreshTokenCommand(
            accessToken,
            refreshToken.Token,
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("jti", jwtId)
        }));

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns(claimsPrincipal);

        MockUserManager
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        MockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        MockRefreshTokenService
            .Setup(x => x.ValidateRefreshTokenAsync(
                refreshToken.Token,
                jwtId,
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        MockRefreshTokenService
            .Setup(x => x.RefreshTokenAsync(
                refreshToken,
                It.IsAny<string>(),
                command.IpAddress,
                command.DeviceInfo,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenEntity?)null);

        var newAccessToken = CreateValidJwtToken();
        var newJwtId = Guid.NewGuid().ToString();
        var newAccessExpiry = DateTime.UtcNow.AddMinutes(15);
        SetupTokenServiceCreateAccessToken(newAccessToken, newJwtId, newAccessExpiry);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Token refresh failed");
    }

    [Fact]
    public async Task Handle_MissingUserIdClaim_ShouldReturnFailure()
    {
        // Arrange
        var jwtId = Guid.NewGuid().ToString();
        var accessToken = CreateValidJwtToken();
        var refreshTokenValue = CreateValidRefreshToken();
        
        var command = new RefreshTokenCommand(
            accessToken,
            refreshTokenValue,
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("jti", jwtId) // Missing NameIdentifier claim
        }));

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns(claimsPrincipal);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid token");
    }

    [Fact]
    public async Task Handle_MissingJtiClaim_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var accessToken = CreateValidJwtToken();
        var refreshTokenValue = CreateValidRefreshToken();
        
        var command = new RefreshTokenCommand(
            accessToken,
            refreshTokenValue,
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId) // Missing jti claim
        }));

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns(claimsPrincipal);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid token");
    }

    [Fact]
    public async Task Handle_ExceptionDuringProcessing_ShouldReturnFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand(
            CreateValidJwtToken(),
            CreateValidRefreshToken(),
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Throws(new Exception("Token service error"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("An error occurred during token refresh");
    }

    [Fact]
    public async Task Handle_NullCommand_ShouldThrowArgumentNullException()
    {
        // Arrange
        RefreshTokenCommand command = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.Handle(command, CreateCancellationToken()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("null")]
    public async Task Handle_EmptyOrNullAccessToken_ShouldReturnFailure(string accessToken)
    {
        // Arrange
        // Convert "null" string to actual null for testing, but use empty string for constructor
        var actualAccessToken = accessToken == "null" ? "" : accessToken;
        var command = new RefreshTokenCommand(
            actualAccessToken,
            CreateValidRefreshToken(),
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken!))
            .Returns((ClaimsPrincipal?)null);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid token");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_InvalidRefreshTokenString_ShouldReturnFailure(string? refreshToken)
    {
        // Arrange
        var user = CreateTestUser();
        var jwtId = Guid.NewGuid().ToString();
        var accessToken = CreateValidJwtToken();
        
        var command = new RefreshTokenCommand(
            accessToken,
            refreshToken!,
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("jti", jwtId)
        }));

        MockTokenService
            .Setup(x => x.GetPrincipalFromExpiredToken(accessToken))
            .Returns(claimsPrincipal);

        MockUserManager
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        MockRefreshTokenService
            .Setup(x => x.ValidateRefreshTokenAsync(
                refreshToken!,
                jwtId,
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenEntity?)null);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid refresh token");
    }
}
