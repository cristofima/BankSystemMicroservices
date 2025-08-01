using BankSystem.ApiGateway.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace BankSystem.ApiGateway.Middleware;

public class SelectiveAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SelectiveAuthenticationMiddleware> _logger;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex[] PublicEndpointPatterns =
    [
        new (@"^/api/v1/auth/(login|register|refresh|forgot-password|reset-password).*", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),
        new (@"^/health.*", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),
        new (@"^/scalar.*", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),
        new (@"^/openapi.*", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout),
        new (@"^/$", RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout)
    ];

    public SelectiveAuthenticationMiddleware(RequestDelegate next, ILogger<SelectiveAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Check if this is a public endpoint
        if (IsPublicEndpoint(path))
        {
            _logger.LogDebug("Public endpoint accessed: {Path}", path);

            // Add rate limiting for auth endpoints
            if (path.Contains("/auth/"))
            {
                context.Request.Headers["X-RateLimit-Policy"] = "auth";
            }

            await _next(context);
            return;
        }

        // For protected endpoints, check authentication
        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            _logger.LogWarning("Unauthenticated access attempt to protected endpoint: {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Authentication required");
            return;
        }

        // Check authorization based on route
        var requiredPolicy = GetRequiredPolicy(path);
        if (!string.IsNullOrEmpty(requiredPolicy) && requiredPolicy != AuthenticationPolicies.AuthenticatedUsers)
        {
            var authService = context.RequestServices.GetRequiredService<IAuthorizationService>();
            var result = await authService.AuthorizeAsync(context.User, requiredPolicy);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Authorization failed for user {User} on path {Path} with policy {Policy}",
                    context.User.Identity?.Name, path, requiredPolicy);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Insufficient permissions");
                return;
            }
        }

        // Add rate limiting based on endpoint type
        AddRateLimitPolicy(context, path);

        await _next(context);
    }

    private static bool IsPublicEndpoint(string path)
    {
        return PublicEndpointPatterns.Any(regex => regex.IsMatch(path));
    }

    private static string GetRequiredPolicy(string path)
    {
        // Check exact matches first
        if (AuthenticationPolicies.RouteToPolicy.TryGetValue(path, out var exactPolicy))
        {
            return exactPolicy;
        }

        // Check pattern matches
        if (path.Contains("/admin/", StringComparison.OrdinalIgnoreCase))
            return AuthenticationPolicies.AdminOnly;

        if (path.Contains("/reports/", StringComparison.OrdinalIgnoreCase) || path.Contains("/audit/", StringComparison.OrdinalIgnoreCase))
            return AuthenticationPolicies.ManagerOrAdmin;

        // Default to authenticated users for all other protected endpoints
        return AuthenticationPolicies.AuthenticatedUsers;
    }

    private static void AddRateLimitPolicy(HttpContext context, string path)
    {
        var policy = path switch
        {
            var p when p.Contains("/auth/") => "auth",
            var p when p.Contains("/transactions/") => "transactions",
            _ => "default"
        };

        context.Request.Headers["X-RateLimit-Policy"] = policy;
    }
}

public static class SelectiveAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseSelectiveAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SelectiveAuthenticationMiddleware>();
    }
}