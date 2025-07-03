using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Security.Application.Configuration;
using Security.Application.Interfaces;

namespace Security.Infrastructure.Services;

/// <summary>
/// Service for logging security-related events for audit and compliance purposes
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly SecurityOptions _securityOptions;

    public SecurityAuditService(
        ILogger<SecurityAuditService> logger,
        IOptions<SecurityOptions> securityOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _securityOptions = securityOptions?.Value ?? throw new ArgumentNullException(nameof(securityOptions));
    }

    public Task LogSuccessfulAuthenticationAsync(string userId, string? ipAddress)
    {
        if (_securityOptions.Audit.LogSuccessfulAuthentication)
        {
            _logger.LogInformation("SECURITY_AUDIT: Successful authentication for user {UserId} from IP {IpAddress} at {Timestamp}",
                userId, ipAddress ?? "unknown", DateTime.UtcNow);
        }

        return Task.CompletedTask;
    }

    public Task LogFailedAuthenticationAsync(string userIdentifier, string? ipAddress, string reason)
    {
        if (_securityOptions.Audit.LogFailedAuthentication)
        {
            _logger.LogWarning("SECURITY_AUDIT: Failed authentication attempt for user {UserIdentifier} from IP {IpAddress} at {Timestamp}. Reason: {Reason}",
                userIdentifier, ipAddress ?? "unknown", DateTime.UtcNow, reason);
        }

        return Task.CompletedTask;
    }

    public Task LogTokenRefreshAsync(string userId, string? ipAddress)
    {
        if (_securityOptions.Audit.LogTokenOperations)
        {
            _logger.LogInformation("SECURITY_AUDIT: Token refresh for user {UserId} from IP {IpAddress} at {Timestamp}",
                userId, ipAddress ?? "unknown", DateTime.UtcNow);
        }

        return Task.CompletedTask;
    }

    public Task LogTokenRevocationAsync(string token, string? ipAddress, string? reason)
    {
        if (_securityOptions.Audit.LogTokenOperations)
        {
            _logger.LogInformation("SECURITY_AUDIT: Token revocation for token {TokenHash} from IP {IpAddress} at {Timestamp}. Reason: {Reason}",
                HashToken(token), ipAddress ?? "unknown", DateTime.UtcNow, reason ?? "not specified");
        }

        return Task.CompletedTask;
    }

    public Task LogPermissionChangeAsync(string userId, string action, string? ipAddress)
    {
        _logger.LogInformation("SECURITY_AUDIT: Permission change for user {UserId} from IP {IpAddress} at {Timestamp}. Action: {Action}",
            userId, ipAddress ?? "unknown", DateTime.UtcNow, action);

        return Task.CompletedTask;
    }

    public Task LogSecurityViolationAsync(string userId, string violation, string? ipAddress)
    {
        _logger.LogWarning("SECURITY_AUDIT: Security violation by user {UserId} from IP {IpAddress} at {Timestamp}. Violation: {Violation}",
            userId, ipAddress ?? "unknown", DateTime.UtcNow, violation);

        return Task.CompletedTask;
    }

    public Task LogUserRegistrationAsync(string userId, string? ipAddress)
    {
        if (_securityOptions.Audit.LogUserOperations)
        {
            _logger.LogInformation("SECURITY_AUDIT: User registration for user {UserId} from IP {IpAddress} at {Timestamp}",
                userId, ipAddress ?? "unknown", DateTime.UtcNow);
        }

        return Task.CompletedTask;
    }

    public Task LogUserLogoutAsync(string userId, string? ipAddress)
    {
        if (_securityOptions.Audit.LogUserOperations)
        {
            _logger.LogInformation("SECURITY_AUDIT: User logout for user {UserId} from IP {IpAddress} at {Timestamp}",
                userId, ipAddress ?? "unknown", DateTime.UtcNow);
        }

        return Task.CompletedTask;
    }

    private static string HashToken(string token)
    {
        // Return only first 8 characters for audit purposes (don't log full token)
        return token.Length > 8 ? token[..8] + "..." : token;
    }
}
