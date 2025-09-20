using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BankSystem.Shared.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BankSystem.Shared.WebApiDefaults.Middlewares;

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
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);

            await HandleExceptionAsync(context, e);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        var statusCode = GetStatusCode(exception);

        var response = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = GetTitle(exception),
            Status = statusCode,
            Detail = "An unexpected error occurred. Please try again later.",
        };

        if (exception is CustomValidationException validationException)
        {
            response.Detail = exception.Message;
            response.Extensions["errors"] = validationException.Errors;
        }

        response.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static int GetStatusCode(Exception exception) =>
        exception switch
        {
            CustomValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

    private static string GetTitle(Exception exception) =>
        exception switch
        {
            CustomValidationException validationException => validationException.Title,
            _ => "Internal Server Error",
        };
}
