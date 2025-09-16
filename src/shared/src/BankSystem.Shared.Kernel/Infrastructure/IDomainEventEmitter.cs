using BankSystem.Shared.Kernel.Common;

namespace BankSystem.Shared.Kernel.Infrastructure;

/// <summary>
/// Defines the contract for domain event emission infrastructure.
/// This service automatically handles domain event publishing from aggregate roots
/// in a way that integrates seamlessly with repository and command handler patterns.
/// </summary>
public interface IDomainEventEmitter
{
    /// <summary>
    /// Emits all pending domain events from the specified aggregate root.
    /// This method should be called after successfully persisting aggregate changes.
    /// </summary>
    /// <param name="aggregateRoot">The aggregate root containing domain events to emit.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EmitEventsAsync(
        IAggregateRoot aggregateRoot,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Emits all pending domain events from multiple aggregate roots.
    /// This method should be called after successfully persisting multiple aggregate changes.
    /// </summary>
    /// <param name="aggregateRoots">The collection of aggregate roots containing domain events to emit.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EmitEventsAsync(
        IEnumerable<IAggregateRoot> aggregateRoots,
        CancellationToken cancellationToken = default
    );
}
