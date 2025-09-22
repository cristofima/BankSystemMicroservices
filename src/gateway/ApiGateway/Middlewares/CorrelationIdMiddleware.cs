using BankSystem.Shared.WebApiDefaults.Constants;

namespace BankSystem.ApiGateway.Middlewares;

/// <summary>
/// Middleware to handle correlation ID generation and propagation for distributed tracing.
/// Ensures every request has a correlation ID for tracking across microservices.
/// </summary>
public class CorrelationIdMiddleware : IMiddleware
{
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(ILogger<CorrelationIdMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add correlation ID to response headers for client tracking
        if (!context.Response.Headers.ContainsKey(HttpHeaderConstants.CorrelationId))
        {
            context.Response.Headers[HttpHeaderConstants.CorrelationId] = correlationId;
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
            await next(context);
        }
    }

    /// <summary>
    /// Gets existing correlation ID from request headers or creates a new one.
    /// </summary>
    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (
            context.Request.Headers.TryGetValue(
                HttpHeaderConstants.CorrelationId,
                out var correlationId
            )
        )
        {
            var correlationIdValue = correlationId.Count > 0 ? correlationId[0] : null;
            if (
                !string.IsNullOrEmpty(correlationIdValue)
                && IsValidCorrelationId(correlationIdValue)
            )
            {
                return correlationIdValue;
            }
        }

        // Generate new correlation ID if none exists or invalid
        var newCorrelationId = Guid.NewGuid().ToString();
        context.Request.Headers[HttpHeaderConstants.CorrelationId] = newCorrelationId;

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
