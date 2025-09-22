using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BankSystem.Shared.Domain.Exceptions;
using BankSystem.Shared.WebApiDefaults.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankSystem.Shared.WebApiDefaults.Middlewares;

/// <summary>
/// Exception handling middleware that combines the best features from both
/// Gateway and Shared middlewares, providing standardized RFC 7807 Problem Details responses
/// with correlation tracking and comprehensive validation error support.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) =>
        _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; no response write.
            _logger.LogDebug("Request was canceled by the client.");
        }
        catch (Exception e)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(e, "Response has already started; skipping ProblemDetails.");
                throw;
            }

            var correlationId = GetCorrelationId(context);

            _logger.LogError(
                e,
                "Unhandled exception while processing {Method} {Path}. TraceId={TraceId}, CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path.Value,
                Activity.Current?.Id ?? context.TraceIdentifier,
                correlationId
            );

            await HandleExceptionAsync(context, e);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var correlationId = GetCorrelationId(context);
        context.Response.Headers[HttpHeaderConstants.CorrelationId] = correlationId;

        var response = new ProblemDetails
        {
            Type = GetProblemType(statusCode),
            Title = GetTitle(exception),
            Status = statusCode,
            Detail = GetDetailMessage(exception),
            Instance = GetDescriptiveInstance(context),
        };

        // Add custom validation errors if applicable
        if (exception is CustomValidationException validationException)
        {
            response.Detail = exception.Message;
            response.Extensions["errors"] = validationException.Errors;
        }

        // Add correlation tracking (from Gateway)
        response.Extensions["correlationId"] = correlationId;
        response.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        if (
            statusCode == StatusCodes.Status401Unauthorized
            && !context.Response.Headers.ContainsKey(HttpHeaderConstants.WwwAuthenticate)
        )
        {
            context.Response.Headers.WWWAuthenticate =
                "Bearer realm=\"api\", error=\"invalid_token\"";
        }

        var jsonOptions = context
            .RequestServices.GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, jsonOptions),
            context.RequestAborted
        );
    }

    private static string GetCorrelationId(HttpContext httpContext)
    {
        if (
            httpContext.Request.Headers.TryGetValue(
                HttpHeaderConstants.CorrelationId,
                out var correlationId
            )
            && correlationId.Count > 0
            && !string.IsNullOrEmpty(correlationId[0])
        )
        {
            return correlationId[0]!;
        }

        return Guid.NewGuid().ToString();
    }

    private static string GetDescriptiveInstance(HttpContext httpContext)
    {
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path;
        return $"{method} {path}";
    }

    private static string GetProblemType(int statusCode) =>
        statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            501 => "https://tools.ietf.org/html/rfc7231#section-6.6.2",
            502 => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
            503 => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            504 => "https://tools.ietf.org/html/rfc7231#section-6.6.5",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        };

    private static string GetDetailMessage(Exception exception)
    {
        return exception switch
        {
            CustomValidationException => exception.Message,
            ArgumentException or ArgumentNullException => exception.Message,
            UnauthorizedAccessException => "Authentication is required to access this resource.",
            KeyNotFoundException => "The requested resource was not found.",
            BadHttpRequestException => exception.Message,
            NotImplementedException => "This feature is not yet implemented.",
            TimeoutException => "The operation timed out. Please try again.",
            _ => "An unexpected error occurred. Please try again later.",
        };
    }

    private static int GetStatusCode(Exception exception) =>
        exception switch
        {
            CustomValidationException => StatusCodes.Status400BadRequest,
            BadHttpRequestException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            TimeoutException => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError,
        };

    private static string GetTitle(Exception exception) =>
        exception switch
        {
            CustomValidationException validationException => validationException.Title,
            BadHttpRequestException => "Bad Request",
            ArgumentException => "Bad Request",
            KeyNotFoundException => "Not Found",
            UnauthorizedAccessException => "Unauthorized",
            NotImplementedException => "Not Implemented",
            TimeoutException => "Gateway Timeout",
            _ => "Internal Server Error",
        };
}
