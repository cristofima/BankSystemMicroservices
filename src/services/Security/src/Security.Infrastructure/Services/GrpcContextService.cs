using BankSystem.Shared.Domain.Validation;
using Microsoft.AspNetCore.Http;
using Security.Application.Interfaces;

namespace Security.Infrastructure.Services;

/// <summary>
/// Implementation of gRPC context service that handles context extraction and correlation IDs
/// </summary>
public class GrpcContextService : IGrpcContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the GrpcContextService
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor</param>
    public GrpcContextService(IHttpContextAccessor httpContextAccessor)
    {
        Guard.AgainstNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Extracts the calling service name from the current context using multiple fallback strategies
    /// </summary>
    /// <returns>The name of the calling service or "Unknown Service" if not found</returns>
    public string GetCallingServiceName()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return "Unknown Service";
        }

        // Strategy 1: Try to get service name from custom header
        if (httpContext.Request.Headers.TryGetValue("x-service-name", out var serviceNameHeader))
        {
            var serviceName = serviceNameHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                return serviceName;
            }
        }

        // Strategy 2: Try to get from JWT claims
        var serviceClaim = httpContext.User?.FindFirst("service_name");
        if (serviceClaim != null && !string.IsNullOrWhiteSpace(serviceClaim.Value))
        {
            return serviceClaim.Value;
        }

        // Strategy 3: Try to get from User-Agent
        if (httpContext.Request.Headers.TryGetValue("User-Agent", out var userAgentHeader))
        {
            var userAgent = userAgentHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                return userAgent;
            }
        }

        // Strategy 4: Try to get from remote address
        var remoteAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        return !string.IsNullOrWhiteSpace(remoteAddress)
            ? $"Service-{remoteAddress}"
            : "Unknown Service";
    }

    /// <summary>
    /// Creates a new correlation ID for request tracking
    /// </summary>
    /// <returns>A new GUID-based correlation ID</returns>
    public string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Creates a standardized logging scope for gRPC operations
    /// </summary>
    /// <param name="serviceName">The name of the gRPC service</param>
    /// <param name="methodName">The name of the gRPC method</param>
    /// <param name="correlationId">The correlation ID for the request</param>
    /// <returns>A dictionary containing the logging scope properties</returns>
    public Dictionary<string, object> CreateLoggingScope(
        string serviceName,
        string methodName,
        string correlationId
    )
    {
        return new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Service"] = serviceName,
            ["Method"] = methodName,
            ["Timestamp"] = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Gets the current HTTP context
    /// </summary>
    /// <returns>The HTTP context</returns>
    public HttpContext? GetHttpContext()
    {
        return _httpContextAccessor.HttpContext;
    }
}
