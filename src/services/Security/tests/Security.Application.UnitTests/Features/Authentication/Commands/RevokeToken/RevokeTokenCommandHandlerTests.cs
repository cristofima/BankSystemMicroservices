using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Security.Application.Features.Authentication.Commands.RevokeToken;
using Security.Application.UnitTests.Common;
using Security.Domain.Common;

namespace Security.Application.UnitTests.Features.Authentication.Commands.RevokeToken;

[TestFixture]
public class RevokeTokenCommandHandlerTests : CommandHandlerTestBase
{
    private RevokeTokenCommandHandler _handler = null!;
    private Mock<ILogger<RevokeTokenCommandHandler>> _mockLogger = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        
        _mockLogger = CreateMockLogger<RevokeTokenCommandHandler>();
        _handler = new RevokeTokenCommandHandler(
            MockRefreshTokenService.Object,
            MockAuditService.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task Handle_ValidTokenRevocation_ShouldReturnSuccess()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var command = new RevokeTokenCommand(
            token,
            CreateValidIpAddress(),
            "User requested revocation");

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
                token,
                command.IpAddress,
                command.Reason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify token revocation
        MockRefreshTokenService.Verify(
            x => x.RevokeTokenAsync(
                token,
                command.IpAddress,
                command.Reason,
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify audit logging
        MockAuditService.Verify(
            x => x.LogTokenRevocationAsync(token, command.IpAddress, command.Reason),
            Times.Once);
    }

    [Test]
    public async Task Handle_TokenRevocationFails_ShouldReturnFailure()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var command = new RevokeTokenCommand(
            token,
            CreateValidIpAddress(),
            "User requested revocation");

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
                token,
                command.IpAddress,
                command.Reason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Token not found or already revoked"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Token not found or already revoked");

        // Verify no audit logging for failed revocation
        MockAuditService.Verify(
            x => x.LogTokenRevocationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_ExceptionDuringProcessing_ShouldReturnFailure()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var command = new RevokeTokenCommand(
            token,
            CreateValidIpAddress(),
            "User requested revocation");

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
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
        result.Error.Should().Be("An error occurred during token revocation");
    }

    [Test]
    public void Handle_NullCommand_ShouldThrowArgumentNullException()
    {
        // Arrange
        RevokeTokenCommand command = null!;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.Handle(command, CreateCancellationToken()));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public async Task Handle_InvalidToken_ShouldReturnFailure(string? token)
    {
        // Arrange
        var command = new RevokeTokenCommand(
            token!,
            CreateValidIpAddress(),
            "User requested revocation");

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
                token!,
                command.IpAddress,
                command.Reason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Invalid token"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid token");
    }

    [Test]
    public async Task Handle_ValidTokenRevocationWithNullReason_ShouldReturnSuccess()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var command = new RevokeTokenCommand(
            token,
            CreateValidIpAddress(),
            null); // Null reason

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
                token,
                command.IpAddress,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify audit logging with null reason
        MockAuditService.Verify(
            x => x.LogTokenRevocationAsync(token, command.IpAddress, null),
            Times.Once);
    }

    [Test]
    public async Task Handle_ValidTokenRevocationWithNullIpAddress_ShouldReturnSuccess()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var command = new RevokeTokenCommand(
            token,
            null, // Null IP address
            "User requested revocation");

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
                token,
                null,
                command.Reason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify audit logging with null IP address
        MockAuditService.Verify(
            x => x.LogTokenRevocationAsync(token, null, command.Reason),
            Times.Once);
    }

    [Test]
    public async Task Handle_AlreadyRevokedToken_ShouldReturnFailure()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var command = new RevokeTokenCommand(
            token,
            CreateValidIpAddress(),
            "User requested revocation");

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
                token,
                command.IpAddress,
                command.Reason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Token is already revoked"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Token is already revoked");
    }

    [Test]
    public async Task Handle_CancellationRequested_ShouldPropagateCancellation()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var command = new RevokeTokenCommand(
            token,
            CreateValidIpAddress(),
            "User requested revocation");

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
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
    public async Task Handle_TokenNotFound_ShouldReturnFailure()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var command = new RevokeTokenCommand(
            token,
            CreateValidIpAddress(),
            "User requested revocation");

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
                token,
                command.IpAddress,
                command.Reason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Token not found"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Token not found");

        // Verify no audit logging for non-existent token
        MockAuditService.Verify(
            x => x.LogTokenRevocationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_ValidRevocationWithLongReason_ShouldReturnSuccess()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var longReason = new string('A', 500); // Very long reason
        var command = new RevokeTokenCommand(
            token,
            CreateValidIpAddress(),
            longReason);

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
                token,
                command.IpAddress,
                longReason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify audit logging with long reason
        MockAuditService.Verify(
            x => x.LogTokenRevocationAsync(token, command.IpAddress, longReason),
            Times.Once);
    }

    [Test]
    public async Task Handle_SecurityAuditServiceThrows_ShouldStillReturnSuccess()
    {
        // Arrange
        var token = CreateValidRefreshToken();
        var command = new RevokeTokenCommand(
            token,
            CreateValidIpAddress(),
            "User requested revocation");

        MockRefreshTokenService
            .Setup(x => x.RevokeTokenAsync(
                token,
                command.IpAddress,
                command.Reason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        MockAuditService
            .Setup(x => x.LogTokenRevocationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("Audit service failed"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("An error occurred during token revocation");
    }
}
