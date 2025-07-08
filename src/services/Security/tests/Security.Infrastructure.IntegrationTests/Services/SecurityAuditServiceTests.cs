using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Security.Application.Configuration;
using Security.Application.Interfaces;
using Security.Infrastructure.Services;

namespace Security.Infrastructure.IntegrationTests.Services;

/// <summary>
/// Integration tests for SecurityAuditService ensuring proper logging behavior
/// and configuration-driven functionality
/// </summary>
public class SecurityAuditServiceTests : IAsyncLifetime
{
    private Mock<ILogger<SecurityAuditService>> _mockLogger = null!;
    private SecurityOptions _securityOptions = null!;
    private ISecurityAuditService _auditService = null!;

    public Task InitializeAsync()
    {
        _mockLogger = new Mock<ILogger<SecurityAuditService>>();
        // Directly instantiate SecurityOptions for tests
        _securityOptions = new SecurityOptions
        {
            Audit = new SecurityOptions.AuditOptions
            {
                EnableAuditLogging = true,
                LogSuccessfulAuthentication = true,
                LogFailedAuthentication = true,
                LogTokenOperations = true,
                LogUserOperations = true
            }
        };
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private void EnsureAuditServiceInitialized()
    {
        // No DI needed, just use the field
        _auditService = new SecurityAuditService(
            _mockLogger.Object,
            Options.Create(_securityOptions));
    }

    #region TokenRevocation Tests

    [Fact]
    public async Task LogTokenRevocationAsync_WhenAuditEnabled_ShouldLogInformation()
    {
        // Arrange
        EnsureAuditServiceInitialized();
        var token = "test-user-123";
        var ipAddress = "RefreshToken";
        var reason = "User logout";

        // Enable both audit logging and token operations
        _securityOptions.Audit.EnableAuditLogging = true;
        _securityOptions.Audit.LogTokenOperations = true;

        // Recreate service with updated options
        _auditService = new SecurityAuditService(
            _mockLogger.Object,
            Options.Create(_securityOptions));

        // Act
        await _auditService.LogTokenRevocationAsync(token, ipAddress, reason);

        // Assert
        VerifyLogCalled(LogLevel.Information, "Token revocation", Times.Once());
        VerifyLogContains(token[..8]);
        VerifyLogContains(reason);
    }

    [Fact]
    public async Task LogTokenRevocationAsync_WhenAuditDisabled_ShouldNotLog()
    {
        // Arrange
        EnsureAuditServiceInitialized();
        var token = "test-user-123";
        var ipAddress = "RefreshToken";
        var reason = "User logout";

        // Disable both audit logging and token operations
        _securityOptions.Audit.EnableAuditLogging = false;
        _securityOptions.Audit.LogTokenOperations = false;

        // Recreate service with updated options
        _auditService = new SecurityAuditService(
            _mockLogger.Object,
            Options.Create(_securityOptions));

        // Act
        await _auditService.LogTokenRevocationAsync(token, ipAddress, reason);

        // Assert
        VerifyNoLogsCalled();
    }

    [Fact]
    public async Task LogTokenRevocationAsync_WithDifferentReasons_ShouldLogCorrectly()
    {
        // Arrange
        EnsureAuditServiceInitialized();
        const string token = "test-user-123";
        const string ipAddress = "RefreshToken";
        _securityOptions.Audit.EnableAuditLogging = true;
        _securityOptions.Audit.LogTokenOperations = true;

        // Recreate service with updated options
        _auditService = new SecurityAuditService(
            _mockLogger.Object,
            Options.Create(_securityOptions));

        var reasons = new[]
        {
            "User logout",
            "Token expired",
            "Security policy violation",
            "Admin revocation",
            "Suspicious activity detected"
        };

        // Act
        foreach (var reason in reasons)
        {
            await _auditService.LogTokenRevocationAsync(token, ipAddress, reason);
        }

        // Assert
        VerifyLogCalled(LogLevel.Information, "Token revocation", Times.Exactly(reasons.Length));

        foreach (var reason in reasons)
        {
            VerifyLogContains(reason);
        }
    }

    #endregion

    #region Configuration Integration Tests

    [Fact]
    public async Task SecurityAuditService_WithAllAuditingDisabled_ShouldNotLogAnything()
    {
        // Arrange - Disable all auditing
        EnsureAuditServiceInitialized();
        _securityOptions.Audit.EnableAuditLogging = false;
        _securityOptions.Audit.LogSuccessfulAuthentication = false;
        _securityOptions.Audit.LogFailedAuthentication = false;
        _securityOptions.Audit.LogTokenOperations = false;
        _securityOptions.Audit.LogUserOperations = false;

        // Recreate service with updated options
        _auditService = new SecurityAuditService(
            _mockLogger.Object,
            Options.Create(_securityOptions));

        const string userId = "test-user";
        const string token = "token";
        const string ipAddress = "0.0.0.0";
        const string reason = "test reason";

        // Act - Call all audit methods
        await _auditService.LogSuccessfulAuthenticationAsync(userId, ipAddress);
        await _auditService.LogFailedAuthenticationAsync(userId, ipAddress, reason);
        await _auditService.LogTokenRefreshAsync(userId, ipAddress);
        await _auditService.LogUserRegistrationAsync(userId, ipAddress);
        await _auditService.LogTokenRevocationAsync(token, ipAddress, reason);
        await _auditService.LogUserLogoutAsync(userId, ipAddress);

        // Assert - No logs should be generated
        VerifyNoLogsCalled();
    }

    #endregion

    #region Additional Logs

    [Fact]
    public async Task LogPermissionChangeAsync_ShouldLogInformation()
    {
        // Arrange
        EnsureAuditServiceInitialized();
        var userId = "perm-user";
        var action = "GrantAdmin";
        var ipAddress = "1.2.3.4";

        // Act
        await _auditService.LogPermissionChangeAsync(userId, action, ipAddress);

        // Assert
        VerifyLogCalled(LogLevel.Information, "Permission change", Times.Once());
        VerifyLogContains(action);
        VerifyLogContains(userId);
    }

    [Fact]
    public async Task LogSecurityViolationAsync_ShouldLogWarning()
    {
        // Arrange
        EnsureAuditServiceInitialized();
        var userId = "violator";
        var violation = "Attempted privilege escalation";
        var ipAddress = "5.6.7.8";

        // Act
        await _auditService.LogSecurityViolationAsync(userId, violation, ipAddress);

        // Assert
        VerifyLogCalled(LogLevel.Warning, "Security violation", Times.Once());
        VerifyLogContains(violation);
        VerifyLogContains(userId);
    }

    [Fact]
    public async Task LogTokenRevocationAsync_ShouldLogShortTokenIfTokenIsShort()
    {
        // Arrange
        EnsureAuditServiceInitialized();
        _securityOptions.Audit.EnableAuditLogging = true;
        _securityOptions.Audit.LogTokenOperations = true;
        var shortToken = "short";
        var ipAddress = "ip";
        var reason = "test";

        // Act
        await _auditService.LogTokenRevocationAsync(shortToken, ipAddress, reason);

        // Assert
        VerifyLogCalled(LogLevel.Information, "Token revocation", Times.Once());
        VerifyLogContains(shortToken);
    }

    [Fact]
    public async Task LogTokenRevocationAsync_ShouldLogHashedTokenIfTokenIsLong()
    {
        // Arrange
        EnsureAuditServiceInitialized();
        _securityOptions.Audit.EnableAuditLogging = true;
        _securityOptions.Audit.LogTokenOperations = true;
        var longToken = "123456789abcdefgh";
        var ipAddress = "ip";
        var reason = "test";

        // Act
        await _auditService.LogTokenRevocationAsync(longToken, ipAddress, reason);

        // Assert
        VerifyLogCalled(LogLevel.Information, "Token revocation", Times.Once());
        VerifyLogContains(longToken[..8]);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Verifies that a log was called with specific level and message content
    /// </summary>
    private void VerifyLogCalled(LogLevel level, string messageContains, Times times)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageContains)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    /// <summary>
    /// Verifies that a log was called with specific level (any number of times)
    /// </summary>
    private void VerifyLogCalled(LogLevel level, Times times)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    /// <summary>
    /// Verifies that logs contain specific parameter values
    /// </summary>
    private void VerifyLogContains(string expectedValue)
    {
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains(expectedValue)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that no logs were called
    /// </summary>
    private void VerifyNoLogsCalled()
    {
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion
}
