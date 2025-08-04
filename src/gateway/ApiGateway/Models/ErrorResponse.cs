using Microsoft.AspNetCore.Mvc;

namespace BankSystem.ApiGateway.Models;

/// <summary>
/// Standardized error response following RFC 7807 Problem Details format.
/// </summary>
public sealed class ErrorResponse : ProblemDetails
{
    /// <summary>
    /// Timestamp when the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}