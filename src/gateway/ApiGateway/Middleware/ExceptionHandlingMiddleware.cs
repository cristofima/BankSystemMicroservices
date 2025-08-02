using BankSystem.ApiGateway.Models;
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

    private static readonly JsonSerializerOptions JsonOptionsDev = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions JsonOptionsProd = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

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
        var response = CreateErrorResponse(exception, context);

        context.Response.StatusCode = response.Status;
        context.Response.ContentType = "application/json";

        var options = _environment.IsDevelopment() ? JsonOptionsDev : JsonOptionsProd;
        var jsonResponse = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Creates standardized error response based on exception type.
    /// Follows RFC 7807 Problem Details specification.
    /// </summary>
    private ErrorResponse CreateErrorResponse(Exception exception, HttpContext context)
    {
        var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault()
                                 ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault();

        return exception switch
        {
            ArgumentException => GetErrorResponse(
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "Bad Request",
                HttpStatusCode.BadRequest,
                "The request contains invalid parameters",
                correlationId
            ),

            UnauthorizedAccessException => GetErrorResponse(
                "https://tools.ietf.org/html/rfc7235#section-3.1",
                "Unauthorized",
                HttpStatusCode.Unauthorized,
                "Authentication is required to access this resource",
                correlationId
            ),

            TaskCanceledException or OperationCanceledException => GetErrorResponse(
                "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                "Request Timeout",
                HttpStatusCode.RequestTimeout,
                "The request timed out",
                correlationId
            ),

            HttpRequestException => GetErrorResponse(
                "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                "Service Unavailable",
                HttpStatusCode.ServiceUnavailable,
                "An upstream service is currently unavailable",
                correlationId
            ),

            _ => GetErrorResponse(
                "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                "Internal Server Error",
                HttpStatusCode.InternalServerError,
                _environment.IsDevelopment()
                    ? $"An unexpected error occurred: {exception.Message}"
                    : "An unexpected error occurred while processing your request",
                correlationId
            ),
        };
    }

    /// <summary>
    /// Gets a unique instance identifier for this specific error occurrence.
    /// </summary>
    private static string GetInstancePath()
    {
        return $"/errors/{Guid.NewGuid()}";
    }

    private static ErrorResponse GetErrorResponse(string type, string title, HttpStatusCode statusCode, string detail, string? correlationId)
    {
        return new ErrorResponse
        {
            Type = type,
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = GetInstancePath(),
            CorrelationId = correlationId
        };
    }
}