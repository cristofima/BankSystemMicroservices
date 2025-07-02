using Security.Domain.Entities;
using Security.Domain.Common;

namespace Security.Application.Interfaces;

/// <summary>
/// Service for managing refresh tokens
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for a user
    /// </summary>
    Task<RefreshToken?> CreateRefreshTokenAsync(
        string userId,
        string jwtId,
        string? ipAddress = null,
        string? deviceInfo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token and returns it if valid
    /// </summary>
    Task<RefreshToken?> ValidateRefreshTokenAsync(
        string token,
        string jwtId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes a token by creating a new one and revoking the old one
    /// </summary>
    Task<RefreshToken?> RefreshTokenAsync(
        RefreshToken oldToken,
        string newJwtId,
        string? ipAddress = null,
        string? deviceInfo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token
    /// </summary>
    Task<Result> RevokeTokenAsync(
        string token,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user
    /// </summary>
    Task<Result> RevokeAllUserTokensAsync(
        string userId,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired tokens
    /// </summary>
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}

