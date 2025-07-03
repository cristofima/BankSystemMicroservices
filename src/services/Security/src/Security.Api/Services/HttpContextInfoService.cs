namespace Security.Api.Services;

/// <summary>
/// Service for extracting HTTP context information
/// </summary>
public interface IHttpContextInfoService
{
    /// <summary>
    /// Gets the client IP address from the HTTP context
    /// </summary>
    /// <returns>Client IP address or unknown if not available</returns>
    string GetClientIpAddress();

    /// <summary>
    /// Gets device information from the User-Agent header
    /// </summary>
    /// <returns>Device information or unknown if not available</returns>
    string GetDeviceInfo();
}

/// <summary>
/// Implementation of HTTP context information service
/// </summary>
public class HttpContextInfoService : IHttpContextInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextInfoService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return "unknown";

        return context.Connection.RemoteIpAddress?.ToString() ??
               context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
               "unknown";
    }

    public string GetDeviceInfo()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return "unknown";

        return context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }
}
