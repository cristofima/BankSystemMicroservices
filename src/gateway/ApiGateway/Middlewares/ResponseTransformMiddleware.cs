using System.Text;
using System.Text.Json;

namespace BankSystem.ApiGateway.Middlewares;

/// <summary>
/// Middleware to transform downstream service responses to RFC 7807 format when needed
/// </summary>
public class ResponseTransformMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTransformMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

    public ResponseTransformMiddleware(
        RequestDelegate next,
        ILogger<ResponseTransformMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store the original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await _next(context);

            // Check if we need to transform the response
            if (ShouldTransformResponse(context))
            {
                await TransformResponseAsync(context, responseBodyStream, originalBodyStream);
            }
            else
            {
                // Copy the response as-is
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool ShouldTransformResponse(HttpContext context)
    {
        // Transform error responses that don't have proper content-type
        if (context.Response.StatusCode is < 400 or >= 600)
            return false;

        var ct = context.Response.ContentType;
        if (string.IsNullOrWhiteSpace(ct))
            return true;

        return !(
            ct.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            || ct.Contains("json", StringComparison.OrdinalIgnoreCase)
            || ct.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase)
        );
    }

    private async Task TransformResponseAsync(
        HttpContext context,
        MemoryStream responseBodyStream,
        Stream originalBodyStream
    )
    {
        try
        {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(responseBodyStream).ReadToEndAsync();

            // Get correlation ID from headers
            var correlationId =
                context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
                ?? context.TraceIdentifier;

            // Create RFC 7807 problem details response
            var problemDetails = new
            {
                type = GetProblemType(context.Response.StatusCode),
                title = GetProblemTitle(context.Response.StatusCode),
                status = context.Response.StatusCode,
                detail = string.IsNullOrWhiteSpace(responseContent)
                    ? GetDefaultDetail(context.Response.StatusCode)
                    : responseContent.Trim(),
                instance = $"{context.Request.Method} {context.Request.Path}",
                correlationId,
                timestamp = DateTime.UtcNow,
            };

            // Serialize to JSON
            var jsonResponse = JsonSerializer.Serialize(problemDetails, JsonOptions);

            // Update response headers
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;

            // Write the transformed response to the original stream
            var jsonBytes = Encoding.UTF8.GetBytes(jsonResponse);
            context.Response.ContentLength = jsonBytes.Length;
            await originalBodyStream.WriteAsync(jsonBytes);

            _logger.LogDebug(
                "Transformed {StatusCode} response to RFC 7807 format for {Method} {Path}",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error transforming response for {Method} {Path}",
                context.Request.Method,
                context.Request.Path
            );

            // Fallback: copy original response to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
    }

    private static string GetProblemType(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            502 => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
            503 => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        };
    }

    private static string GetProblemTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            _ => "Error",
        };
    }

    private static string GetDefaultDetail(int statusCode)
    {
        return statusCode switch
        {
            400 => "The request was invalid or cannot be served",
            401 => "Authentication is required to access this resource",
            403 => "You don't have permission to access this resource",
            404 => "The requested resource was not found",
            409 => "The request conflicts with the current state of the resource",
            422 => "The request was well-formed but contains semantic errors",
            500 => "An internal server error occurred",
            502 => "The gateway received an invalid response from the upstream server",
            503 => "The service is temporarily unavailable",
            _ => "An error occurred while processing the request",
        };
    }
}
