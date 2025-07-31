namespace BankSystem.ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring security headers in the API Gateway.
/// Implements OWASP-recommended security headers for web applications.
/// Centralized security headers for all microservices behind the gateway.
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds security headers middleware to the application pipeline.
    /// Implements headers recommended by OWASP for secure web applications.
    /// Includes environment-aware policies and header existence checks.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="environment">The web host environment for environment-specific policies</param>
    /// <returns>The application builder for method chaining</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IWebHostEnvironment? environment = null)
    {
        return app.Use(async (context, next) =>
        {
            // Set security headers after response starts but before content is written
            context.Response.OnStarting(() =>
            {
                SetSecurityHeaders(context, environment);
                return Task.CompletedTask;
            });

            await next(context);
        });
    }

    /// <summary>
    /// Sets security headers on the response with environment-aware policies.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="environment">The web host environment</param>
    private static void SetSecurityHeaders(HttpContext context, IWebHostEnvironment? environment)
    {
        var headers = context.Response.Headers;
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var isDevelopment = environment?.IsDevelopment() ?? false;

        // Remove server information disclosure
        headers.Remove("Server");

        // Basic security headers - check existence to avoid overwrites
        if (!headers.ContainsKey("X-Content-Type-Options"))
            headers.XContentTypeOptions = "nosniff";

        if (!headers.ContainsKey("X-Frame-Options"))
            headers.XFrameOptions = "DENY";

        if (!headers.ContainsKey("X-XSS-Protection"))
            headers.XXSSProtection = "1; mode=block";

        if (!headers.ContainsKey("Referrer-Policy"))
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content Security Policy - environment-aware with documentation endpoint handling
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            if (isDevelopment)
            {
                // Skip CSP for documentation endpoints in development to avoid conflicts
                if (path.Contains("/scalar") || path.Contains("/openapi") || path.Contains("/swagger"))
                {
                    // Don't set CSP for documentation endpoints
                }
                else
                {
                    // Development CSP - less restrictive for debugging
                    headers.ContentSecurityPolicy =
                        "default-src 'self'; " +
                        "script-src 'self'; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data:; " +
                        "font-src 'self'; " +
                        "connect-src 'self' https:; " +
                        "frame-src 'none'; " +
                        "object-src 'none'; " +
                        "base-uri 'self'; " +
                        "form-action 'self'";
                }
            }
            else
            {
                // Production CSP - strict security policy
                headers.ContentSecurityPolicy =
                    "default-src 'none'; " +
                    "script-src 'self'; " +
                    "style-src 'self'; " +
                    "img-src 'self' data:; " +
                    "font-src 'self'; " +
                    "connect-src 'self'; " +
                    "frame-src 'none'; " +
                    "object-src 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'; " +
                    "upgrade-insecure-requests";
            }
        }

        // Permissions Policy - disable potentially dangerous features
        if (!headers.ContainsKey("Permissions-Policy"))
        {
            headers["Permissions-Policy"] = "camera=(), " +
                                          "microphone=(), " +
                                          "geolocation=(), " +
                                          "payment=(), " +
                                          "usb=(), " +
                                          "bluetooth=(), " +
                                          "magnetometer=(), " +
                                          "gyroscope=(), " +
                                          "accelerometer=()";
        }

        // Cache control for API responses
        if (headers.ContainsKey("Cache-Control")) return;
        headers.CacheControl = "no-cache, no-store, must-revalidate";
        headers.Pragma = "no-cache";
        headers.Expires = "0";
    }

    /// <summary>
    /// Adds strict security headers middleware for production environments.
    /// Implements the most restrictive security policies with HSTS.
    /// Use this method for high-security environments.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="maxAge">HSTS max-age in seconds (default: 1 year)</param>
    /// <returns>The application builder for method chaining</returns>
    public static IApplicationBuilder UseStrictSecurityHeaders(this IApplicationBuilder app, int maxAge = 31536000)
    {
        return app.Use(async (context, next) =>
        {
            // Set security headers before processing the request
            context.Response.OnStarting(() =>
            {
                SetStrictSecurityHeaders(context, maxAge);
                return Task.CompletedTask;
            });

            await next(context);
        });
    }

    /// <summary>
    /// Sets the most restrictive security headers for production environments.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="maxAge">HSTS max-age in seconds</param>
    private static void SetStrictSecurityHeaders(HttpContext context, int maxAge)
    {
        var headers = context.Response.Headers;

        // Remove server information disclosure
        headers.Remove("Server");

        // Basic security headers
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers.XXSSProtection = "1; mode=block";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Strict Transport Security (HSTS) - only for HTTPS
        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = $"max-age={maxAge}; includeSubDomains; preload";
        }

        // Most restrictive Content Security Policy for production
        headers.ContentSecurityPolicy =
            "default-src 'none'; " +
            "script-src 'self'; " +
            "style-src 'self'; " +
            "img-src 'self'; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-src 'none'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "upgrade-insecure-requests";

        // Permissions Policy - disable all potentially dangerous features
        headers["Permissions-Policy"] = "camera=(), " +
                                      "microphone=(), " +
                                      "geolocation=(), " +
                                      "payment=(), " +
                                      "usb=(), " +
                                      "bluetooth=(), " +
                                      "magnetometer=(), " +
                                      "gyroscope=(), " +
                                      "accelerometer=(), " +
                                      "fullscreen=(), " +
                                      "picture-in-picture=()";

        // Strict cache control
        headers.CacheControl = "no-cache, no-store, must-revalidate, private";
        headers.Pragma = "no-cache";
        headers.Expires = "0";
    }
}