using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Security.Api.Filters;

/// <summary>
/// Global exception filter for consistent error handling across the API
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception occurred. Path: {Path}, Method: {Method}",
            context.HttpContext.Request.Path,
            context.HttpContext.Request.Method);

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An error occurred while processing your request",
            Status = StatusCodes.Status500InternalServerError,
            Instance = context.HttpContext.Request.Path,
            Detail = context.HttpContext.RequestServices
                .GetService<IWebHostEnvironment>()?.IsDevelopment() == true
                ? context.Exception.Message
                : "An unexpected error occurred"
        };

        // Add correlation ID if available
        if (context.HttpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId.ToString();
        }

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }
}
