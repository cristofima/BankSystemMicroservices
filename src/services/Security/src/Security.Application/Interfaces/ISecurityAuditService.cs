namespace Security.Application.Interfaces;

/// <summary>
/// Service for logging security-related events for audit purposes
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// Logs successful authentication
    /// </summary>
    Task LogSuccessfulAuthenticationAsync(string userId, string? ipAddress);

    /// <summary>
    /// Logs failed authentication attempt
    /// </summary>
    Task LogFailedAuthenticationAsync(string userIdentifier, string? ipAddress, string reason);

    /// <summary>
    /// Logs token refresh event
    /// </summary>
    Task LogTokenRefreshAsync(string userId, string? ipAddress);

    /// <summary>
    /// Logs token revocation event
    /// </summary>
    Task LogTokenRevocationAsync(string token, string? ipAddress, string? reason);

    /// <summary>
    /// Logs permission changes
    /// </summary>
    Task LogPermissionChangeAsync(string userId, string action, string? ipAddress);

    /// <summary>
    /// Logs security violations
    /// </summary>
    Task LogSecurityViolationAsync(string userId, string violation, string? ipAddress);

    /// <summary>
    /// Logs user registration event
    /// </summary>
    Task LogUserRegistrationAsync(string userId, string? ipAddress);

    /// <summary>
    /// Logs user logout event
    /// </summary>
    Task LogUserLogoutAsync(string userId, string? ipAddress);
}
