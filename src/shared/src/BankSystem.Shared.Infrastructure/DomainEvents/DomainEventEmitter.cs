using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Infrastructure;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using MediatRMediator = MediatR.IMediator;

namespace BankSystem.Shared.Infrastructure.DomainEvents;

/// <summary>
/// Simplified domain event emitter that only handles MediatR domain events.
/// Cross-microservice communication is handled by specific domain event handlers.
/// </summary>
public class DomainEventEmitter : IDomainEventEmitter
{
    private readonly MediatRMediator _mediator;
    private readonly ILogger<DomainEventEmitter> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public DomainEventEmitter(MediatRMediator mediator, ILogger<DomainEventEmitter> logger)
    {
        Guard.AgainstNull(mediator, nameof(mediator));
        Guard.AgainstNull(logger, nameof(logger));

        _mediator = mediator;
        _logger = logger;

        // Configure retry policy for resilience
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Domain event publishing failed (attempt {RetryCount}). Retrying in {Delay}ms. Error: {Error}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        outcome.Message
                    );
                }
            );
    }

    /// <summary>
    /// Emits all domain events from an aggregate root
    /// </summary>
    public async Task EmitEventsAsync(
        IAggregateRoot aggregateRoot,
        CancellationToken cancellationToken = default
    )
    {
        if (aggregateRoot?.DomainEvents == null || aggregateRoot.DomainEvents.Count == 0)
        {
            return;
        }

        var events = aggregateRoot.DomainEvents.ToList();

        _logger.LogInformation(
            "Emitting {EventCount} domain events for aggregate {AggregateType}",
            events.Count,
            aggregateRoot.GetType().Name
        );

        await _retryPolicy.ExecuteAsync(async () =>
        {
            foreach (var domainEvent in events)
            {
                _logger.LogDebug(
                    "Publishing domain event {EventType} with ID {EventId}",
                    domainEvent.GetType().Name,
                    domainEvent.EventId
                );

                await _mediator.Publish(domainEvent, cancellationToken);
            }
        });

        // Clear events only after successful emission
        aggregateRoot.ClearDomainEvents();

        _logger.LogInformation("Successfully emitted {EventCount} domain events", events.Count);
    }

    /// <summary>
    /// Emits domain events from multiple aggregate roots
    /// </summary>
    public async Task EmitEventsAsync(
        IEnumerable<IAggregateRoot> aggregateRoots,
        CancellationToken cancellationToken = default
    )
    {
        if (!aggregateRoots.Any())
        {
            return;
        }

        var allEvents = aggregateRoots
            .Where(ar => ar.DomainEvents.Count > 0)
            .SelectMany(ar => ar.DomainEvents)
            .ToList();

        if (allEvents.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Emitting {EventCount} domain events from {AggregateCount} aggregates",
            allEvents.Count,
            aggregateRoots.Count()
        );

        await _retryPolicy.ExecuteAsync(async () =>
        {
            foreach (var domainEvent in allEvents)
            {
                _logger.LogDebug(
                    "Publishing domain event {EventType} with ID {EventId}",
                    domainEvent.GetType().Name,
                    domainEvent.EventId
                );

                await _mediator.Publish(domainEvent, cancellationToken);
            }
        });

        // Clear events only after successful emission
        foreach (var aggregateRoot in aggregateRoots.Where(ar => ar.DomainEvents.Count > 0))
        {
            aggregateRoot.ClearDomainEvents();
        }

        _logger.LogInformation("Successfully emitted {EventCount} domain events", allEvents.Count);
    }
}
