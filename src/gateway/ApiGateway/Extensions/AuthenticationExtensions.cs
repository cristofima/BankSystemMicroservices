namespace BankSystem.ApiGateway.Extensions;

/// <summary>
/// Extension methods for selective authentication middleware configuration.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Define paths that don't require authentication (public endpoints)
    /// </summary>
    private static readonly string[] PublicPaths =
    [
        "/health",
        "/health/ready",
        "/health/live",
        "/scalar",
        "/api/docs",
        "/.well-known",
        "/api/v1/auth/login",
        "/api/v1/auth/register",
        "/api/v1/auth/refresh",
        "/api/v1/auth/forgot-password",
        "/api/v1/auth/reset-password",
    ];

    /// <summary>
    /// Configures selective authentication middleware that allows optional authentication
    /// for certain routes while requiring it for others.
    /// </summary>
    /// <param name="app">The web application builder</param>
    /// <returns>The configured application</returns>
    public static IApplicationBuilder UseSelectiveAuthentication(this IApplicationBuilder app)
    {
        return app.Use(
            async (context, next) =>
            {
                var path = context.Request.Path.Value;
                var isPublicPath = PublicPaths.Any(p =>
                    path?.StartsWith(p, StringComparison.OrdinalIgnoreCase) == true
                );

                // For public paths, allow anonymous access
                if (isPublicPath)
                {
                    await next();
                    return;
                }

                // For OPTIONS requests (CORS preflight), always allow
                if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    await next();
                    return;
                }

                // For all other paths, continue with normal auth pipeline
                // The authentication/authorization middleware will handle the logic
                await next();
            }
        );
    }
}
