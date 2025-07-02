using Microsoft.Extensions.Caching.Memory;
using Security.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Security.Api.Middleware;

/// <summary>
/// Middleware to check if tokens have been revoked
/// Based on the token revocation pattern from the article
/// </summary>
public class TokenRevocationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TokenRevocationMiddleware> _logger;

    public TokenRevocationMiddleware(
        RequestDelegate next,
        IMemoryCache memoryCache,
        ILogger<TokenRevocationMiddleware> logger)
    {
        _next = next;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip revocation check for certain paths
        if (ShouldSkipRevocationCheck(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Only check authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jwtId = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            
            if (!string.IsNullOrEmpty(jwtId))
            {
                // Check if this token has been revoked
                if (_memoryCache.TryGetValue($"revoked_token_{jwtId}", out _))
                {
                    _logger.LogWarning("Blocked request with revoked token {JwtId} from IP {IpAddress}", 
                        jwtId, GetClientIpAddress(context));
                    
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Token has been revoked");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool ShouldSkipRevocationCheck(PathString path)
    {
        var pathsToSkip = new[]
        {
            "/api/v1/auth/login",
            "/api/v1/auth/refresh",
            "/api/v1/auth/register",
            "/health",
            "/swagger",
            "/scalar"
        };

        return pathsToSkip.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ??
               context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault();
    }
}

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
