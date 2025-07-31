using System.ComponentModel.DataAnnotations;

namespace BankSystem.ApiGateway.Configuration;

/// <summary>
/// Configuration options for API Gateway health checks
/// </summary>
public class GatewayHealthCheckOptions
{
    public const string SectionName = "HealthChecks";

    /// <summary>
    /// Health check timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Self health check configuration
    /// </summary>
    public SelfHealthCheck Self { get; set; } = new();

    /// <summary>
    /// External service health checks
    /// </summary>
    public List<ServiceHealthCheck> Services { get; set; } = new();

    /// <summary>
    /// Self health check configuration
    /// </summary>
    public class SelfHealthCheck
    {
        /// <summary>
        /// Name of the self health check
        /// </summary>
        public string Name { get; set; } = "self";

        /// <summary>
        /// Health check message
        /// </summary>
        public string Message { get; set; } = "The API Gateway is healthy";
    }

    /// <summary>
    /// External service health check configuration
    /// </summary>
    public class ServiceHealthCheck
    {
        /// <summary>
        /// Service name for identification
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Health check endpoint URI
        /// </summary>
        [Required]
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// Display name for the health check
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Timeout in seconds for this specific check (overrides global timeout)
        /// </summary>
        public int? TimeoutSeconds { get; set; }

        /// <summary>
        /// Failure status when health check fails
        /// </summary>
        public string FailureStatus { get; set; } = "Degraded";

        /// <summary>
        /// Tags for grouping health checks
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }
}