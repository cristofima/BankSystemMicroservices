using System.Collections.Concurrent;
using System.Reflection;
using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Kernel.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankSystem.Shared.Infrastructure.DomainEvents;

/// <summary>
/// Domain event dispatcher implementing Outbox pattern best practices.
/// Provides reliable domain event publishing through MassTransit's Entity Framework Outbox integration.
/// Events are stored in database tables and delivered by background services for guaranteed delivery.
/// </summary>
public class DbContextDomainEventDispatcher : IDbContextDomainEventDispatcher
{
    private readonly ILogger<DbContextDomainEventDispatcher> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IdPropertyCache = new();

    private static readonly string[] ActionPatterns =
    [
        "Created",
        "Updated",
        "Deleted",
        "Activated",
        "Deactivated",
        "Suspended",
        "Closed",
        "Opened",
        "Processed",
        "Completed",
        "Failed",
        "Cancelled",
        "Approved",
        "Rejected",
    ];

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
        Guard.AgainstNull(logger);
        Guard.AgainstNull(publishEndpoint);

        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Dispatches domain events using the Outbox pattern with proper DbContext integration.
    /// Events are stored in database tables and delivered by background services for guaranteed delivery.
    /// Events are not cleared from aggregates to allow EF Core to process them in the outbox pattern.
    /// </summary>
    /// <param name="dbContext">Entity Framework DbContext for transaction coordination.</param>
    /// <param name="aggregatesWithEvents">Collection of aggregate roots containing domain events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Task representing the asynchronous dispatch operation.</returns>
    public async Task DispatchEventsWithDbContextAsync(
        DbContext dbContext,
        IEnumerable<IAggregateRoot> aggregatesWithEvents,
        CancellationToken cancellationToken = default
    )
    {
        var aggregateList = aggregatesWithEvents.ToList();
        if (aggregateList.Count == 0)
        {
            _logger.LogDebug("No aggregates provided for dispatch.");
            return;
        }

        _logger.LogDebug(
            "Starting enhanced outbox dispatch for {Count} aggregates",
            aggregateList.Count
        );

        try
        {
            aggregateList = aggregateList
                .Where(aggregate => aggregate.DomainEvents.Count > 0)
                .ToList();

            var eventCount = aggregateList.Sum(a => a.DomainEvents.Count);
            _logger.LogDebug(
                "Starting enhanced outbox dispatch for {AggregateCount} aggregates with {EventCount} events",
                aggregateList.Count,
                eventCount
            );

            if (eventCount == 0)
            {
                _logger.LogDebug("No domain events to dispatch.");
                return;
            }

            foreach (var aggregate in aggregateList)
            {
                cancellationToken.ThrowIfCancellationRequested();
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
                                context.MessageId = domainEvent.EventId;
                                context.CorrelationId = domainEvent.EventId;
                                context.TimeToLive = TimeSpan.FromDays(7);

                                // Add custom headers for routing and filtering
                                context.Headers.Set("EventType", eventType);
                                context.Headers.Set(
                                    "AggregateId",
                                    GetAggregateId(aggregate) ?? "Unknown"
                                );
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

            var totalEvents = aggregateList.Sum(a => a.DomainEvents.Count);
            _logger.LogInformation(
                "Successfully stored {EventCount} domain events from {AggregateCount} aggregates in Outbox for background processing (events preserved for EF Core)",
                totalEvents,
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
    /// Extracts the source service name from the event type name using naming conventions.
    /// Assumes event naming pattern: {Service}{Action}Event (e.g., "AccountCreatedEvent" -> "Account")
    /// </summary>
    /// <param name="eventType">The full name of the event type</param>
    /// <returns>The extracted service name or the event type without "Event" suffix if no pattern matches</returns>
    private static string GetSourceFromEventType(string eventType)
    {
        // Extract service name from event type (e.g., "AccountCreatedEvent" -> "Account")
        // This assumes event naming convention: {Service}{Action}Event
        if (!eventType.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
            return eventType;

        // Remove "Event" suffix and try to extract service name
        var withoutEvent = eventType[..^5]; // Remove "Event"

        foreach (var pattern in ActionPatterns)
        {
            if (withoutEvent.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return withoutEvent[..^pattern.Length];
            }
        }

        // If no pattern found, return the event name without "Event"
        return withoutEvent;
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
            var aggregateType = aggregate.GetType();
            var idProperty = IdPropertyCache.GetOrAdd(
                aggregateType,
                type => type.GetProperty("Id")
            );
            return idProperty?.GetValue(aggregate);
        }
        catch
        {
            // If we can't get the ID, return null
            return null;
        }
    }
}
