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
    private const string UnknownValue = "unknown";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextInfoService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return UnknownValue;

        return context.Connection.RemoteIpAddress?.ToString() ??
               context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
               UnknownValue;
    }

    public string GetDeviceInfo()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return UnknownValue;

        return context.Request.Headers.UserAgent.FirstOrDefault() ?? UnknownValue;
    }
}
