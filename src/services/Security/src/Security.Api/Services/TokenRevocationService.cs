using Microsoft.Extensions.Caching.Memory;

namespace Security.Api.Services;

/// <summary>
/// Service to manage revoked tokens in memory cache
/// </summary>
public interface ITokenRevocationService
{
    Task RevokeTokenAsync(string jwtId, TimeSpan? expiry = null);
    Task<bool> IsTokenRevokedAsync(string jwtId);
    Task ClearExpiredTokensAsync();
}

/// <summary>
/// Implementation of token revocation service using memory cache
/// </summary>
public class TokenRevocationService : ITokenRevocationService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TokenRevocationService> _logger;

    public TokenRevocationService(IMemoryCache memoryCache, ILogger<TokenRevocationService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task RevokeTokenAsync(string jwtId, TimeSpan? expiry = null)
    {
        var cacheKey = $"revoked_token_{jwtId}";
        var expiryTime = expiry ?? TimeSpan.FromHours(24); // Default to 24 hours

        _memoryCache.Set(cacheKey, DateTime.UtcNow, expiryTime);

        _logger.LogInformation("Token {JwtId} added to revocation cache", jwtId);

        return Task.CompletedTask;
    }

    public Task<bool> IsTokenRevokedAsync(string jwtId)
    {
        var cacheKey = $"revoked_token_{jwtId}";
        var isRevoked = _memoryCache.TryGetValue(cacheKey, out _);

        return Task.FromResult(isRevoked);
    }

    public Task ClearExpiredTokensAsync()
    {
        // Memory cache automatically handles expiration
        // This method could be used for manual cleanup if needed
        _logger.LogDebug("Token revocation cache cleanup completed");

        return Task.CompletedTask;
    }
}

