using BankSystem.Shared.Domain.Validation;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatcher"/> class.
    /// </summary>
    public DomainEventDispatcher(
        IMediator mediator,
        IPublishEndpoint publishEndpoint,
        ILogger<DomainEventDispatcher> logger
    )
    {
        Guard.AgainstNull(mediator, nameof(mediator));
        Guard.AgainstNull(publishEndpoint, nameof(publishEndpoint));
        Guard.AgainstNull(logger, nameof(logger));

        _mediator = mediator;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task DispatchEventsAsync(
        IAggregateRoot aggregateRoot,
        CancellationToken cancellationToken = default
    )
    {
        if (aggregateRoot.DomainEvents.Count == 0)
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

    /// <inheritdoc/>
    public async Task DispatchEventsAsync(
        IEnumerable<IAggregateRoot> aggregateRoots,
        CancellationToken cancellationToken = default
    )
    {
        var aggregates = aggregateRoots.Where(ar => ar.DomainEvents.Count > 0).ToList();
        if (aggregates.Count == 0)
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

    /// <inheritdoc/>
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
