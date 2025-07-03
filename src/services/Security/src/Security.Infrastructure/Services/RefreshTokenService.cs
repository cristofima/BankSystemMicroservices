using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Security.Application.Configuration;
using Security.Application.Interfaces;
using Security.Domain.Common;
using Security.Domain.Entities;
using Security.Infrastructure.Data;
using System.Security.Cryptography;

namespace Security.Infrastructure.Services;

/// <summary>
/// Service for managing refresh tokens with proper security and audit tracking
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly SecurityDbContext _context;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly JwtOptions _jwtOptions;
    private readonly SecurityOptions _securityOptions;

    public RefreshTokenService(
        SecurityDbContext context,
        ILogger<RefreshTokenService> logger,
        IOptions<JwtOptions> jwtOptions,
        IOptions<SecurityOptions> securityOptions)
    {
        _context = context;
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
        _securityOptions = securityOptions.Value;
    }

    public async Task<RefreshToken?> CreateRefreshTokenAsync(
        string userId,
        string jwtId,
        string? ipAddress = null,
        string? deviceInfo = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user has reached max concurrent sessions
            if (_securityOptions.TokenSecurity.MaxConcurrentSessions > 0)
            {
                var currentTime = DateTime.UtcNow;
                var activeTokensCount = await _context.RefreshTokens
                    .CountAsync(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiryDate > currentTime, cancellationToken);

                if (activeTokensCount >= _securityOptions.TokenSecurity.MaxConcurrentSessions)
                {
                    // Revoke oldest token to make room
                    var oldestToken = await _context.RefreshTokens
                        .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiryDate > currentTime)
                        .OrderBy(rt => rt.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (oldestToken != null)
                    {
                        oldestToken.Revoke(ipAddress, "Exceeded maximum concurrent sessions");
                        _logger.LogInformation("Revoked oldest token for user {UserId} due to session limit", userId);
                    }
                }
            }

            var refreshToken = new RefreshToken
            {
                Token = GenerateSecureToken(),
                JwtId = jwtId,
                UserId = userId,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryInDays),
                CreatedByIp = ipAddress,
                DeviceInfo = deviceInfo,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created refresh token for user {UserId} from IP {IpAddress}", 
                userId, ipAddress);

            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refresh token for user {UserId}", userId);
            return null;
        }
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(
        string token,
        string jwtId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

            if (refreshToken == null)
            {
                _logger.LogWarning("Invalid refresh token attempted for user {UserId}", userId);
                return null;
            }

            // Check if token belongs to the user and JWT
            if (refreshToken.UserId != userId || refreshToken.JwtId != jwtId)
            {
                _logger.LogWarning("Refresh token mismatch for user {UserId}", userId);
                return null;
            }

            // Check if token is expired
            if (refreshToken.IsExpired)
            {
                _logger.LogWarning("Expired refresh token used for user {UserId}", userId);
                return null;
            }

            // Check if token is revoked
            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning("Revoked refresh token used for user {UserId}", userId);
                return null;
            }

            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token for user {UserId}", userId);
            return null;
        }
    }

    public async Task<RefreshToken?> RefreshTokenAsync(
        RefreshToken oldToken,
        string newJwtId,
        string? ipAddress = null,
        string? deviceInfo = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Revoke the old token
            oldToken.Revoke(ipAddress, "Token rotated");

            // Create new token
            var newToken = new RefreshToken
            {
                Token = GenerateSecureToken(),
                JwtId = newJwtId,
                UserId = oldToken.UserId,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryInDays),
                CreatedByIp = ipAddress,
                DeviceInfo = deviceInfo ?? oldToken.DeviceInfo,
                ReplacedByToken = null, // Will be set when this token is replaced
                CreatedAt = DateTime.UtcNow,
                CreatedBy = oldToken.UserId
            };

            // Set the replacement chain
            oldToken.ReplacedByToken = newToken.Token;

            _context.RefreshTokens.Add(newToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refreshed token for user {UserId} from IP {IpAddress}", 
                oldToken.UserId, ipAddress);

            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for user {UserId}", oldToken.UserId);
            return null;
        }
    }

    public async Task<Result> RevokeTokenAsync(
        string token,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

            if (refreshToken == null)
            {
                _logger.LogWarning("Attempted to revoke non-existent token from IP {IpAddress}", ipAddress);
                return Result.Failure("Token not found");
            }

            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning("Attempted to revoke already revoked token from IP {IpAddress}", ipAddress);
                return Result.Failure("Token already revoked");
            }

            refreshToken.Revoke(ipAddress, reason ?? "Manual revocation");
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Revoked token for user {UserId} from IP {IpAddress}. Reason: {Reason}", 
                refreshToken.UserId, ipAddress, reason);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token from IP {IpAddress}", ipAddress);
            return Result.Failure("Error revoking token");
        }
    }

    public async Task<Result> RevokeAllUserTokensAsync(
        string userId,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            if (!activeTokens.Any())
            {
                _logger.LogInformation("No active tokens found for user {UserId}", userId);
                return Result.Success();
            }

            foreach (var token in activeTokens)
            {
                token.Revoke(ipAddress, reason ?? "All user tokens revoked");
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Revoked {TokenCount} tokens for user {UserId} from IP {IpAddress}. Reason: {Reason}", 
                activeTokens.Count, userId, ipAddress, reason);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            return Result.Failure("Error revoking user tokens");
        }
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_securityOptions.TokenSecurity.CleanupExpiredTokensAfterDays);

            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiryDate < cutoffDate || 
                           (rt.IsRevoked && rt.RevokedAt < cutoffDate))
                .ToListAsync(cancellationToken);

            if (expiredTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {TokenCount} expired/revoked tokens", expiredTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token cleanup");
        }
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
