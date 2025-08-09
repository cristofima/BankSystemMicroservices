using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Infrastructure;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using MediatRDomainEvent = BankSystem.Shared.Kernel.Events.IDomainEvent;

namespace BankSystem.Shared.Infrastructure.DomainEvents;

/// <summary>
/// Unified domain event dispatcher that handles both local MediatR events
/// and cross-service publishing via MassTransit
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IMediator mediator,
        IPublishEndpoint publishEndpoint,
        ILogger<DomainEventDispatcher> logger
    )
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _publishEndpoint =
            publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchEventsAsync(
        IAggregateRoot aggregateRoot,
        CancellationToken cancellationToken = default
    )
    {
        if (aggregateRoot?.DomainEvents?.Any() != true)
            return;

        var events = aggregateRoot.DomainEvents.ToList();

        _logger.LogDebug("Dispatching {EventCount} domain events for aggregate", events.Count);

        foreach (var domainEvent in events)
        {
            await DispatchEventAsync(domainEvent, cancellationToken);
        }

        // Clear events after successful dispatch
        aggregateRoot.ClearDomainEvents();

        _logger.LogInformation(
            "Successfully dispatched {EventCount} domain events for aggregate",
            events.Count
        );
    }

    public async Task DispatchEventsAsync(
        IEnumerable<IAggregateRoot> aggregateRoots,
        CancellationToken cancellationToken = default
    )
    {
        var aggregates = aggregateRoots?.Where(ar => ar.DomainEvents?.Any() == true).ToList();
        if (aggregates?.Any() != true)
            return;

        _logger.LogDebug(
            "Dispatching domain events from {AggregateCount} aggregates",
            aggregates.Count
        );

        foreach (var aggregate in aggregates)
        {
            await DispatchEventsAsync(aggregate, cancellationToken);
        }
    }

    public async Task DispatchEventAsync(
        MediatRDomainEvent domainEvent,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug(
                "Dispatching domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.EventId
            );

            // 1. First publish locally via MediatR for in-process handlers
            await _mediator.Publish(domainEvent, cancellationToken);

            // 2. Then publish via MassTransit for cross-service communication
            // MassTransit will serialize and route the event to other services
            await _publishEndpoint.Publish(domainEvent, cancellationToken);

            _logger.LogDebug(
                "Successfully dispatched domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.EventId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to dispatch domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.EventId
            );
            throw;
        }
    }
}
