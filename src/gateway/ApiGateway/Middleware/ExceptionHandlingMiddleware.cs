using System.Net;
using System.Text.Json;

namespace BankSystem.ApiGateway.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and returns consistent error responses following RFC 7807 Problem Details format.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unhandled exception occurred. RequestPath: {RequestPath}, Method: {Method}", 
                context.Request.Path, 
                context.Request.Method);
            
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exceptions and converts them to appropriate HTTP responses.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = CreateErrorResponse(exception);

        context.Response.StatusCode = response.Status;
        context.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Creates standardized error response based on exception type.
    /// Follows RFC 7807 Problem Details specification.
    /// </summary>
    private ErrorResponse CreateErrorResponse(Exception exception)
    {
        return exception switch
        {
            ArgumentException argumentEx => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "The request contains invalid parameters",
                Instance = GetInstancePath()
            },
            
            UnauthorizedAccessException => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Unauthorized",
                Status = (int)HttpStatusCode.Unauthorized,
                Detail = "Authentication is required to access this resource",
                Instance = GetInstancePath()
            },
            
            TaskCanceledException or OperationCanceledException => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Request Timeout",
                Status = (int)HttpStatusCode.RequestTimeout,
                Detail = "The request timed out",
                Instance = GetInstancePath()
            },
            
            HttpRequestException httpEx => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = "Service Unavailable",
                Status = (int)HttpStatusCode.ServiceUnavailable,
                Detail = "An upstream service is currently unavailable",
                Instance = GetInstancePath()
            },
            
            _ => new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = _environment.IsDevelopment() 
                    ? $"An unexpected error occurred: {exception.Message}"
                    : "An unexpected error occurred while processing your request",
                Instance = GetInstancePath()
            }
        };
    }

    /// <summary>
    /// Gets a unique instance identifier for this specific error occurrence.
    /// </summary>
    private static string GetInstancePath()
    {
        return $"/errors/{Guid.NewGuid()}";
    }
}

/// <summary>
/// Standardized error response following RFC 7807 Problem Details format.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// A URI reference that identifies the problem type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// A short, human-readable summary of the problem type.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP status code for this occurrence of the problem.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem.
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }
}
