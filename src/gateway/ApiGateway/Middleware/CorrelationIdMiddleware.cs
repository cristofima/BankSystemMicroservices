namespace BankSystem.ApiGateway.Middleware;

/// <summary>
/// Middleware that ensures every request has a correlation ID for distributed tracing.
/// Creates a new correlation ID if one doesn't exist or validates the existing one.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add correlation ID to response headers for client tracking
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

        // Add correlation ID to logging context
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            _logger.LogDebug("Processing request with correlation ID: {CorrelationId}", correlationId);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed with correlation ID: {CorrelationId}", correlationId);
                throw;
            }
        }
    }

    /// <summary>
    /// Gets existing correlation ID from request headers or creates a new one.
    /// Validates format and creates new ID if invalid.
    /// </summary>
    private string GetOrCreateCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();

        if (IsValidCorrelationId(correlationId))
        {
            _logger.LogDebug("Using existing correlation ID: {CorrelationId}", correlationId);
            return correlationId!;
        }

        // Create new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString();
        context.Request.Headers[CorrelationIdHeaderName] = newCorrelationId;

        _logger.LogDebug("Created new correlation ID: {CorrelationId}", newCorrelationId);
        return newCorrelationId;
    }

    /// <summary>
    /// Validates correlation ID format (should be a valid GUID).
    /// </summary>
    private static bool IsValidCorrelationId(string? correlationId)
    {
        return !string.IsNullOrWhiteSpace(correlationId) &&
               Guid.TryParse(correlationId, out _);
    }
}
