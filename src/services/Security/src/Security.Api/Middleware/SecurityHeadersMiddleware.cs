namespace Security.Api.Middleware;

/// <summary>
/// Middleware for setting security headers with appropriate CSP for development tools
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Set security headers after response starts but before content is written
        context.Response.OnStarting(() =>
        {
            SetSecurityHeaders(context);
            return Task.CompletedTask;
        });
        
        await _next(context);
    }

    private void SetSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Basic security headers
        if (!headers.ContainsKey("X-Content-Type-Options"))
            headers["X-Content-Type-Options"] = "nosniff";
        
        if (!headers.ContainsKey("X-Frame-Options"))
            headers["X-Frame-Options"] = "DENY";
        
        if (!headers.ContainsKey("X-XSS-Protection"))
            headers["X-XSS-Protection"] = "1; mode=block";
        
        if (!headers.ContainsKey("Referrer-Policy"))
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content Security Policy - skip for documentation endpoints in development
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            if (_environment.IsDevelopment())
            {
                // Check if this is a documentation endpoint - skip CSP entirely
                if (path.Contains("/scalar") || path.Contains("/openapi") || path.Contains("/swagger"))
                {
                    // Don't set CSP for documentation endpoints to avoid conflicts
                    // Scalar will work better without CSP restrictions
                }
                else
                {
                    // Standard development CSP for API endpoints
                    headers["Content-Security-Policy"] = 
                        "default-src 'self'; " +
                        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data: https:; " +
                        "font-src 'self' https:; " +
                        "connect-src 'self' https: wss:; " +
                        "frame-src 'self'";
                }
            }
            else
            {
                // Strict CSP for production
                headers["Content-Security-Policy"] = 
                    "default-src 'self'; " +
                    "script-src 'self'; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data:; " +
                    "font-src 'self'; " +
                    "connect-src 'self'; " +
                    "frame-src 'none'";
            }
        }

        // Remove server header for security
        headers.Remove("Server");
        
        // Add Permissions Policy (formerly Feature Policy) if not already set
        if (!headers.ContainsKey("Permissions-Policy"))
        {
            headers["Permissions-Policy"] = 
                "camera=(), " +
                "microphone=(), " +
                "geolocation=(), " +
                "payment=(), " +
                "usb=(), " +
                "magnetometer=(), " +
                "gyroscope=(), " +
                "accelerometer=()";
        }
    }
}
