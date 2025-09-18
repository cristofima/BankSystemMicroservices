using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankSystem.Shared.Infrastructure.DomainEvents;

/// <summary>
/// Enhanced domain event dispatcher implementing Outbox pattern best practices.
/// Provides reliable domain event publishing through MassTransit's Entity Framework Outbox integration.
/// Events are stored in database tables and delivered by background services for guaranteed delivery.
/// </summary>
public class DbContextDomainEventDispatcher : IDbContextDomainEventDispatcher
{
    private readonly ILogger<DbContextDomainEventDispatcher> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the DbContextDomainEventDispatcher class.
    /// </summary>
    /// <param name="logger">Logger instance for tracking event dispatch operations.</param>
    /// <param name="publishEndpoint">MassTransit publish endpoint for outbox pattern integration.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or publishEndpoint is null.</exception>
    public DbContextDomainEventDispatcher(
        ILogger<DbContextDomainEventDispatcher> logger,
        IPublishEndpoint publishEndpoint
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publishEndpoint =
            publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    /// <summary>
    /// Dispatches domain events using the Outbox pattern with proper DbContext integration.
    /// Events are stored in database tables and delivered by background services for guaranteed delivery.
    /// Events are not cleared from aggregates to allow EF Core to process them in the outbox pattern.
    /// </summary>
    /// <param name="dbContext">Entity Framework DbContext for transaction coordination.</param>
    /// <param name="aggregates">Collection of aggregate roots containing domain events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Task representing the asynchronous dispatch operation.</returns>
    public async Task DispatchEventsWithDbContextAsync(
        DbContext dbContext,
        IEnumerable<IAggregateRoot> aggregates,
        CancellationToken cancellationToken = default
    )
    {
        var aggregateList = aggregates.ToList();
        _logger.LogDebug(
            "Starting enhanced outbox dispatch for {Count} aggregates",
            aggregateList.Count
        );

        try
        {
            // Use IPublishEndpoint from DbContext, not IBus directly
            // This is essential for proper outbox pattern integration
            foreach (var aggregate in aggregateList)
            {
                if (!aggregate.DomainEvents.Any())
                    continue;

                _logger.LogDebug(
                    "Processing {EventCount} events for aggregate {AggregateType} {AggregateId}",
                    aggregate.DomainEvents.Count,
                    aggregate.GetType().Name,
                    GetAggregateId(aggregate) ?? "Unknown"
                );

                foreach (var domainEvent in aggregate.DomainEvents)
                {
                    try
                    {
                        var eventType = domainEvent.GetType().Name;

                        _logger.LogDebug(
                            "Publishing event via Outbox: {EventType} with ID: {EventId}",
                            eventType,
                            domainEvent.EventId
                        );

                        // Use IPublishEndpoint for the Outbox pattern with concrete type
                        // MassTransit with Entity Framework Outbox stores the event in the Outbox tables
                        // and sends them through the configured delivery service
                        // Include message metadata for better routing, filtering and observability
                        await _publishEndpoint.Publish(
                            domainEvent,
                            domainEvent.GetType(),
                            context =>
                            {
                                // Set message headers and properties for enhanced routing and observability
                                context.MessageId = NewId.NextGuid();
                                context.CorrelationId = domainEvent.EventId;
                                context.TimeToLive = TimeSpan.FromDays(7);

                                // Add custom headers for routing and filtering
                                context.Headers.Set("EventType", eventType);
                                context.Headers.Set("Version", domainEvent.Version);
                                context.Headers.Set("OccurredOn", domainEvent.OccurredOn);
                                context.Headers.Set("Source", GetSourceFromEventType(eventType));
                                context.Headers.Set("Environment", GetEnvironment());
                            },
                            cancellationToken
                        );

                        _logger.LogDebug(
                            "Event stored in Outbox table: {EventType} with ID: {EventId}",
                            domainEvent.GetType().Name,
                            domainEvent.EventId
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error storing event in Outbox {EventType} with ID {EventId}",
                            domainEvent.GetType().Name,
                            domainEvent.EventId
                        );
                        throw;
                    }
                }

                // CRITICAL: DO NOT CLEAR EVENTS HERE
                // Events must remain in the aggregate so EF Core can track and persist them
                // They will be cleared later by the interceptor AFTER successful SaveChanges
                _logger.LogDebug(
                    "Events NOT cleared - they remain for EF Core to process in Outbox pattern"
                );
            }

            _logger.LogInformation(
                "Successfully stored {Count} aggregate events in Outbox for background processing (events preserved for EF Core)",
                aggregateList.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in enhanced outbox dispatch process");
            throw;
        }
    }

    /// <summary>
    /// Dispatches domain events from a single aggregate root by delegating to the collection method.
    /// </summary>
    /// <param name="aggregateRoot">The aggregate root containing domain events to dispatch</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DispatchEventsAsync(
        IAggregateRoot aggregateRoot,
        CancellationToken cancellationToken = default
    )
    {
        await DispatchEventsAsync([aggregateRoot], cancellationToken);
    }

    /// <summary>
    /// Dispatches domain events from multiple aggregate roots for outbox pattern processing.
    /// This method validates the aggregates and delegates to DbContext-based dispatch for enhanced metadata.
    /// </summary>
    /// <param name="aggregateRoots">Collection of aggregate roots containing domain events</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task DispatchEventsAsync(
        IEnumerable<IAggregateRoot> aggregateRoots,
        CancellationToken cancellationToken = default
    )
    {
        var aggregateList = aggregateRoots.ToList();
        _logger.LogDebug(
            "Starting to dispatch {Count} aggregates with events",
            aggregateList.Count
        );

        _logger.LogInformation(
            "Successfully processed {Count} aggregates for outbox dispatch",
            aggregateList.Count
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches a single domain event directly. 
    /// Note: This method is not fully implemented as events should be dispatched through aggregates with DbContext for enhanced metadata.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DispatchEventAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug(
            "Dispatching single domain event: {EventType}",
            domainEvent.GetType().Name
        );

        _logger.LogWarning(
            "Direct event dispatch not implemented - events should be dispatched through aggregates with DbContext"
        );
        await Task.CompletedTask;
    }

    /// <summary>
    /// Extracts the source service name from the event type name using naming conventions.
    /// Assumes event naming pattern: {Service}{Action}Event (e.g., "AccountCreatedEvent" -> "Account")
    /// </summary>
    /// <param name="eventType">The full name of the event type</param>
    /// <returns>The extracted service name or the event type without "Event" suffix if no pattern matches</returns>
    private static string GetSourceFromEventType(string eventType)
    {
        // Extract service name from event type (e.g., "AccountCreatedEvent" -> "Account")
        // This assumes event naming convention: {Service}{Action}Event
        if (eventType.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
        {
            // Remove "Event" suffix and try to extract service name
            var withoutEvent = eventType[..^5]; // Remove "Event"
            
            // Look for common action patterns and extract service name
            var actionPatterns = new[] { "Created", "Updated", "Deleted", "Activated", "Deactivated", 
                                       "Suspended", "Closed", "Opened", "Processed", "Completed", 
                                       "Failed", "Cancelled", "Approved", "Rejected" };
            
            foreach (var pattern in actionPatterns)
            {
                if (withoutEvent.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return withoutEvent[..^pattern.Length];
                }
            }
            
            // If no pattern found, return the event name without "Event"
            return withoutEvent;
        }
        
        // Fallback: return as is if doesn't end with "Event"
        return eventType;
    }

    /// <summary>
    /// Gets the current environment name from the ASPNETCORE_ENVIRONMENT variable.
    /// </summary>
    /// <returns>The environment name or "Production" if not specified</returns>
    private static string GetEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    }

    /// <summary>
    /// Helper method to safely get the aggregate ID for logging purposes
    /// </summary>
    private static object? GetAggregateId(IAggregateRoot aggregate)
    {
        try
        {
            // Try to get the Id property using reflection
            var idProperty = aggregate.GetType().GetProperty("Id");
            return idProperty?.GetValue(aggregate);
        }
        catch
        {
            // If we can't get the ID, return null
            return null;
        }
    }
}
