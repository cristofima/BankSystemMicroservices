using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;

namespace Security.Api.Middleware;

/// <summary>
/// Middleware to check if tokens have been revoked
/// Based on the token revocation pattern from the article
/// </summary>
[ExcludeFromCodeCoverage]
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

        // Only check authenticated requests with valid JWT ID
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jwtId = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jwtId) && _memoryCache.TryGetValue($"revoked_token_{jwtId}", out _))
            {
                _logger.LogWarning("Blocked request with revoked token {JwtId} from IP {IpAddress}",
                    jwtId, GetClientIpAddress(context));

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Token has been revoked");
                return;
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