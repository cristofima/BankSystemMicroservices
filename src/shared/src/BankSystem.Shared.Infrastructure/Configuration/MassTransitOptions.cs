using System.ComponentModel.DataAnnotations;

namespace BankSystem.Shared.Infrastructure.Configuration;

/// <summary>
/// Configuration options for MassTransit messaging system.
/// Provides settings for outbox pattern, Azure Service Bus transport, and message handling.
/// </summary>
public class MassTransitOptions
{
    /// <summary>
    /// Configuration section name in application settings.
    /// </summary>
    public const string SectionName = "MassTransit";

    /// <summary>
    /// Outbox pattern configuration for reliable message publishing.
    /// </summary>
    public OutboxConfiguration Outbox { get; set; } = new();

    /// <summary>
    /// Azure Service Bus basic configuration settings.
    /// </summary>
    public AzureServiceBusConfiguration AzureServiceBus { get; set; } = new();

    /// <summary>
    /// Retry configuration for handling transient failures.
    /// </summary>
    public RetryConfiguration Retry { get; set; } = new();

    /// <summary>
    /// Circuit breaker configuration for fault tolerance.
    /// </summary>
    public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();
}

/// <summary>
/// MassTransit configuration options for Entity Framework Outbox pattern.
/// </summary>
public class OutboxConfiguration
{
    /// <summary>
    /// How often to check for pending outbox messages.
    /// Lower values provide faster delivery but increase database load.
    /// </summary>
    [Range(1, 60)]
    public int QueryDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Time window for detecting duplicate messages.
    /// Messages within this window are considered duplicates and ignored.
    /// </summary>
    [Range(5, 10)]
    public int DuplicateDetectionWindowMinutes { get; set; } = 5;

    /// <summary>
    /// Disable inbox cleanup service if not needed.
    /// The cleanup service removes old processed messages from the inbox.
    /// </summary>
    public bool DisableInboxCleanupService { get; set; } = false;
}

/// <summary>
/// Azure Service Bus configuration settings.
/// </summary>
public class AzureServiceBusConfiguration
{
    /// <summary>
    /// Azure Service Bus connection string. Required for production environments.
    /// </summary>
    [Required(ErrorMessage = "Azure Service Bus connection string is required")]
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// Message retry configuration for handling transient failures.
/// </summary>
public class RetryConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts. Default is 5.
    /// </summary>
    [Range(1, 20, ErrorMessage = "Retry limit must be between 1 and 20")]
    public int RetryLimit { get; set; } = 5;

    /// <summary>
    /// Initial retry interval in seconds. Default is 1 second.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Initial retry interval must be between 1 and 60 seconds")]
    public int InitialRetryIntervalSeconds { get; set; } = 1;

    /// <summary>
    /// Maximum retry interval in seconds. Default is 30 seconds.
    /// </summary>
    [Range(1, 300, ErrorMessage = "Max retry interval must be between 1 and 300 seconds")]
    public int MaxRetryIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Retry interval increment for exponential backoff in seconds. Default is 1 second.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Retry interval increment must be between 1 and 60 seconds")]
    public int RetryIntervalIncrementSeconds { get; set; } = 1;
}

/// <summary>
/// Circuit breaker configuration for fault tolerance.
/// Uses the circuit breaker pattern to prevent cascade failures.
/// </summary>
public class CircuitBreakerConfiguration
{
    /// <summary>
    /// Number of failures required to trip the circuit breaker. Default is 5.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Trip threshold must be between 1 and 100")]
    public int TripThreshold { get; set; } = 5;

    /// <summary>
    /// Minimum number of requests in tracking period to enable circuit breaker. Default is 5.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Active threshold must be between 1 and 100")]
    public int ActiveThreshold { get; set; } = 5;

    /// <summary>
    /// Time in minutes before attempting to reset circuit breaker. Default is 5 minutes.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Reset interval must be between 1 and 60 minutes")]
    public int ResetIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Time period in minutes for tracking failures. Default is 1 minute.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Tracking period must be between 1 and 60 minutes")]
    public int TrackingPeriodMinutes { get; set; } = 1;
}
