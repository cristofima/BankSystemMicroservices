namespace BankSystem.Shared.Kernel.Events;

/// <summary>
/// Defines the contract for publishing domain events across the system.
/// This interface resides in Shared.Kernel to maintain clear separation between
/// domain contracts and messaging infrastructure implementations.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a single domain event asynchronously.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events in a batch asynchronously.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to publish.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishBatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default
    );
}
