using BankSystem.Shared.Domain.Validation;

namespace Security.Api.Services;

/// <summary>
/// Service for extracting HTTP context information
/// </summary>
public class HttpContextInfoService
{
    private const string UnknownValue = "unknown";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextInfoService(IHttpContextAccessor httpContextAccessor)
    {
        Guard.AgainstNull(httpContextAccessor);

        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the client IP address from the HTTP context
    /// </summary>
    /// <returns>Client IP address or unknown if not available</returns>
    public string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return UnknownValue;

        return context.Connection.RemoteIpAddress?.ToString()
            ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? UnknownValue;
    }

    /// <summary>
    /// Gets device information from the User-Agent header
    /// </summary>
    /// <returns>Device information or unknown if not available</returns>
    public string GetDeviceInfo()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return UnknownValue;

        return context.Request.Headers.UserAgent.FirstOrDefault() ?? UnknownValue;
    }
}
