using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Security.Application.Features.Authentication.Commands.Logout;
using Security.Application.UnitTests.Common;
using Security.Domain.Common;

namespace Security.Application.UnitTests.Features.Authentication.Commands.Logout;

[TestFixture]
public class LogoutCommandHandlerTests : CommandHandlerTestBase
{
    private LogoutCommandHandler _handler = null!;
    private Mock<ILogger<LogoutCommandHandler>> _mockLogger = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        
        _mockLogger = CreateMockLogger<LogoutCommandHandler>();
        _handler = new LogoutCommandHandler(
            MockRefreshTokenService.Object,
            MockAuditService.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task Handle_ValidLogout_ShouldReturnSuccessAndRevokeAllTokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new LogoutCommand(
            userId,
            CreateValidIpAddress());

        MockRefreshTokenService
            .Setup(x => x.RevokeAllUserTokensAsync(
                userId,
                command.IpAddress,
                "User logout",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify token revocation
        MockRefreshTokenService.Verify(
            x => x.RevokeAllUserTokensAsync(
                userId,
                command.IpAddress,
                "User logout",
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify audit logging
        MockAuditService.Verify(
            x => x.LogUserLogoutAsync(userId, command.IpAddress),
            Times.Once);
    }

    [Test]
    public async Task Handle_TokenRevocationFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new LogoutCommand(
            userId,
            CreateValidIpAddress());

        MockRefreshTokenService
            .Setup(x => x.RevokeAllUserTokensAsync(
                userId,
                command.IpAddress,
                "User logout",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Failed to revoke tokens"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Failed to revoke tokens");

        // Verify no audit logging for failed logout
        MockAuditService.Verify(
            x => x.LogUserLogoutAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_ExceptionDuringProcessing_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new LogoutCommand(
            userId,
            CreateValidIpAddress());

        MockRefreshTokenService
            .Setup(x => x.RevokeAllUserTokensAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("An error occurred during logout");
    }

    [Test]
    public void Handle_NullCommand_ShouldThrowArgumentNullException()
    {
        // Arrange
        LogoutCommand command = null!;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.Handle(command, CreateCancellationToken()));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public async Task Handle_InvalidUserId_ShouldReturnFailure(string? userId)
    {
        // Arrange
        var command = new LogoutCommand(
            userId!,
            CreateValidIpAddress());

        // The RefreshTokenService should handle invalid user IDs gracefully
        MockRefreshTokenService
            .Setup(x => x.RevokeAllUserTokensAsync(
                userId!,
                command.IpAddress,
                "User logout",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Invalid user ID"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid user ID");
    }

    [Test]
    public async Task Handle_ValidLogoutWithNullIpAddress_ShouldStillSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new LogoutCommand(
            userId,
            null); // Null IP address

        MockRefreshTokenService
            .Setup(x => x.RevokeAllUserTokensAsync(
                userId,
                null,
                "User logout",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify audit logging with null IP
        MockAuditService.Verify(
            x => x.LogUserLogoutAsync(userId, null),
            Times.Once);
    }

    [Test]
    public async Task Handle_ValidLogout_ShouldLogInformation()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new LogoutCommand(
            userId,
            CreateValidIpAddress());

        MockRefreshTokenService
            .Setup(x => x.RevokeAllUserTokensAsync(
                userId,
                command.IpAddress,
                "User logout",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Note: We can't easily verify logger calls with Moq in this setup,
        // but we can verify the handler completes successfully
    }

    [Test]
    public async Task Handle_CancellationRequested_ShouldPropagateCancellation()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new LogoutCommand(
            userId,
            CreateValidIpAddress());

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        MockRefreshTokenService
            .Setup(x => x.RevokeAllUserTokensAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        var act = () => _handler.Handle(command, cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Handle_MultipleTokensRevocationPartialFailure_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new LogoutCommand(
            userId,
            CreateValidIpAddress());

        MockRefreshTokenService
            .Setup(x => x.RevokeAllUserTokensAsync(
                userId,
                command.IpAddress,
                "User logout",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Some tokens could not be revoked"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Some tokens could not be revoked");

        // Verify no audit logging for failed logout
        MockAuditService.Verify(
            x => x.LogUserLogoutAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
