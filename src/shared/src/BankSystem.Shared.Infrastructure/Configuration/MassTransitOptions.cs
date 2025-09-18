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
    /// Azure Service Bus transport configuration settings.
    /// </summary>
    public AzureServiceBusTransportConfiguration AzureServiceBus { get; set; } = new();
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
/// MassTransit configuration options for Azure Service Bus transport.
/// Provides comprehensive settings for message queuing, retry policies, circuit breakers, and performance tuning.
/// </summary>
public class AzureServiceBusTransportConfiguration
{
    /// <summary>
    /// Azure Service Bus connection string. Required for production environments.
    /// </summary>
    [Required(ErrorMessage = "Azure Service Bus connection string is required")]
    public string ConnectionString { get; set; } = string.Empty;

    // Transport Configuration
    /// <summary>
    /// Transport-specific configuration settings for Azure Service Bus.
    /// </summary>
    public TransportConfiguration Transport { get; set; } = new();

    // Retry Configuration
    /// <summary>
    /// Message retry configuration for handling transient failures.
    /// </summary>
    public RetryConfiguration Retry { get; set; } = new();

    // Timeout Configuration
    /// <summary>
    /// Timeout settings for various operations.
    /// </summary>
    public TimeoutConfiguration Timeout { get; set; } = new();

    // Performance Configuration
    /// <summary>
    /// Performance and concurrency settings.
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();

    // Circuit Breaker Configuration
    /// <summary>
    /// Circuit breaker configuration for fault tolerance.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Gets or sets the detailed circuit breaker configuration settings.
    /// Provides configuration options for failure thresholds, timeout settings,
    /// and recovery behavior when the circuit breaker is enabled.
    /// </summary>
    public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();

    // Development Configuration
    /// <summary>
    /// Development environment specific settings.
    /// </summary>
    public DevelopmentConfiguration Development { get; set; } = new();
}

/// <summary>
/// Transport-specific configuration for Azure Service Bus.
/// </summary>
public class TransportConfiguration
{
    /// <summary>
    /// Enable topic partitioning for better scalability. Default is false.
    /// </summary>
    public bool EnablePartitioning { get; set; } = false;

    /// <summary>
    /// Require sessions for FIFO message processing. Default is false.
    /// </summary>
    public bool RequiresSession { get; set; } = false;

    /// <summary>
    /// Maximum number of delivery attempts before moving to dead letter queue. Default is 5.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max delivery count must be between 1 and 100")]
    public int MaxDeliveryCount { get; set; } = 5;

    /// <summary>
    /// Duration (in minutes) that a message lock is held. Default is 5 minutes.
    /// </summary>
    [Range(1, 300, ErrorMessage = "Lock duration must be between 1 and 300 minutes")]
    public int LockDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Default topic name for domain events. Can be overridden per message type.
    /// </summary>
    [Required]
    public string DefaultTopicName { get; set; } = string.Empty;

    /// <summary>
    /// Default subscription name for this service instance.
    /// </summary>
    [Required]
    public string DefaultSubscriptionName { get; set; } = string.Empty;
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
/// Timeout configuration for various operations.
/// </summary>
public class TimeoutConfiguration
{
    /// <summary>
    /// Request timeout in seconds. Default is 30 seconds.
    /// </summary>
    [Range(1, 300, ErrorMessage = "Request timeout must be between 1 and 300 seconds")]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Message time to live in minutes. Default is 1440 minutes (24 hours).
    /// </summary>
    [Range(1, 20160, ErrorMessage = "Message TTL must be between 1 minute and 14 days")]
    public int MessageTimeToLiveMinutes { get; set; } = 1440; // 24 hours
}

/// <summary>
/// Performance and concurrency configuration.
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// Number of messages to prefetch for better throughput. Default is 32.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Prefetch count must be between 1 and 1000")]
    public int PrefetchCount { get; set; } = 32;

    /// <summary>
    /// Maximum number of concurrent messages being processed. Default is 16.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Concurrent message limit must be between 1 and 100")]
    public int ConcurrentMessageLimit { get; set; } = 16;

    /// <summary>
    /// Maximum number of concurrent calls to message handlers. Default is 16.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max concurrent calls must be between 1 and 100")]
    public int MaxConcurrentCalls { get; set; } = 16;
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

/// <summary>
/// Development environment specific configuration.
/// </summary>
public class DevelopmentConfiguration
{
    /// <summary>
    /// Use in-memory transport instead of Azure Service Bus for development. Default is true.
    /// </summary>
    public bool UseInMemoryForDevelopment { get; set; }

    /// <summary>
    /// Enable detailed logging for debugging purposes. Default is false.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}
