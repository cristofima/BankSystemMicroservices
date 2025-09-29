using Microsoft.AspNetCore.Mvc;

namespace Security.Api.Services;

/// <summary>
/// Service for creating standardized API responses
/// </summary>
public class ApiResponseService
{
    /// <summary>
    /// Creates a standardized ProblemDetails response
    /// </summary>
    /// <param name="title">Title of the problem</param>
    /// <param name="detail">Detail description</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="instance">Request path instance</param>
    /// <returns>Standardized ProblemDetails object</returns>
    public ProblemDetails CreateProblemDetails(
        string title,
        string detail,
        int statusCode,
        string? instance = null
    )
    {
        return new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = instance,
        };
    }

    /// <summary>
    /// Creates an authentication failed ProblemDetails response
    /// </summary>
    /// <param name="detail">Error detail</param>
    /// <param name="instance">Request path instance</param>
    /// <returns>ProblemDetails for authentication failure</returns>
    public ProblemDetails CreateAuthenticationFailedResponse(string detail, string? instance = null)
    {
        return CreateProblemDetails("Authentication Failed", detail, 401, instance);
    }

    /// <summary>
    /// Creates a validation failed ProblemDetails response
    /// </summary>
    /// <param name="detail">Error detail</param>
    /// <param name="instance">Request path instance</param>
    /// <returns>ProblemDetails for validation failure</returns>
    public ProblemDetails CreateValidationFailedResponse(string detail, string? instance = null)
    {
        return CreateProblemDetails("Validation Failed", detail, 400, instance);
    }

    /// <summary>
    /// Creates a resource not found ProblemDetails response
    /// </summary>
    /// <param name="detail">Error detail</param>
    /// <param name="instance">Request path instance</param>
    /// <returns>ProblemDetails for not found</returns>
    public ProblemDetails CreateNotFoundResponse(string detail, string? instance = null)
    {
        return CreateProblemDetails("Resource Not Found", detail, 404, instance);
    }
}
