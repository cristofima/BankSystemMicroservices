using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Events;

namespace BankSystem.Shared.Kernel.Infrastructure;

/// <summary>
/// Interface for dispatching domain events both locally and cross-service
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches domain events from an aggregate root
    /// </summary>
    Task DispatchEventsAsync(
        IAggregateRoot aggregateRoot,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Dispatches domain events from multiple aggregate roots
    /// </summary>
    Task DispatchEventsAsync(
        IEnumerable<IAggregateRoot> aggregateRoots,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Dispatches a single domain event
    /// </summary>
    Task DispatchEventAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default
    );
}
