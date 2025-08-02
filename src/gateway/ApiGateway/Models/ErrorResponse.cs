namespace BankSystem.ApiGateway.Models;

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