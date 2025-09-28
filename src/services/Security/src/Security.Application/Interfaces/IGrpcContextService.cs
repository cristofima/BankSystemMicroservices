using Microsoft.AspNetCore.Http;

namespace Security.Application.Interfaces;

/// <summary>
/// Service to extract and manage context information following clean architecture principles
/// </summary>
public interface IGrpcContextService
{
    /// <summary>
    /// Extracts the calling service name from the current context
    /// </summary>
    /// <returns>The name of the calling service or "Unknown Service" if not found</returns>
    string GetCallingServiceName();

    /// <summary>
    /// Creates a correlation ID for request tracking
    /// </summary>
    /// <returns>A new correlation ID</returns>
    string CreateCorrelationId();

    /// <summary>
    /// Creates a logging scope for operations
    /// </summary>
    /// <param name="serviceName">The name of the service</param>
    /// <param name="methodName">The name of the method</param>
    /// <param name="correlationId">The correlation ID for the request</param>
    /// <returns>A dictionary containing the logging scope properties</returns>
    Dictionary<string, object> CreateLoggingScope(string serviceName, string methodName, string correlationId);

    /// <summary>
    /// Gets the current HTTP context
    /// </summary>
    /// <returns>The HTTP context</returns>
    HttpContext? GetHttpContext();
}