namespace BankSystem.ApiGateway.Middleware;

/// <summary>
/// Middleware to handle correlation ID generation and propagation for distributed tracing.
/// Ensures every request has a correlation ID for tracking across microservices.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private static readonly string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var correlationId = GetOrCreateCorrelationId(httpContext);

        // Add correlation ID to response headers for client tracking
        if (!httpContext.Response.Headers.ContainsKey(CorrelationIdHeaderName))
        {
            httpContext.Response.Headers[CorrelationIdHeaderName] = correlationId;
        }

        // Add to logging context
        using (
            _logger.BeginScope(new Dictionary<string, object> { ["correlationId"] = correlationId })
        )
        {
            _logger.LogDebug(
                "Processing request with correlation ID: {CorrelationId}",
                correlationId
            );
            await _next(httpContext);
        }
    }

    /// <summary>
    /// Gets existing correlation ID from request headers or creates a new one.
    /// </summary>
    private static string GetOrCreateCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
        {
            var correlationIdValue = correlationId.ToString();
            if (IsValidCorrelationId(correlationIdValue))
            {
                return correlationIdValue;
            }
        }

        // Generate new correlation ID if none exists or invalid
        var newCorrelationId = Guid.NewGuid().ToString();
        httpContext.Request.Headers[CorrelationIdHeaderName] = newCorrelationId;

        return newCorrelationId;
    }

    /// <summary>
    /// Validates if the correlation ID has a proper format.
    /// </summary>
    private static bool IsValidCorrelationId(string correlationId)
    {
        return !string.IsNullOrWhiteSpace(correlationId)
            && correlationId.Length <= 128
            && Guid.TryParse(correlationId, out _);
    }
}
