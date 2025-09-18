using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BankSystem.Shared.Infrastructure.DomainEvents;

/// <summary>
/// Defines an extended domain event dispatcher interface that integrates with Entity Framework Core DbContext
/// to support the Outbox pattern for reliable domain event publishing within database transactions.
/// This interface extends the base IDomainEventDispatcher to provide transactional consistency guarantees.
/// </summary>
/// <remarks>
/// This interface is specifically designed for scenarios where domain events need to be published
/// as part of the same database transaction that persists aggregate changes. The Outbox pattern
/// implementation ensures that both data changes and event publishing are atomically committed,
/// preventing scenarios where data is saved but events fail to publish or vice versa.
/// </remarks>
public interface IDbContextDomainEventDispatcher : IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches domain events from aggregate root entities within the same database transaction
    /// using the Outbox pattern to ensure transactional consistency between data persistence and event publishing.
    /// </summary>
    /// <param name="dbContext">The Entity Framework Core DbContext instance that provides the database transaction context for atomic operations</param>
    /// <param name="aggregatesWithEvents">The collection of aggregate root entities that contain unpublished domain events requiring dispatch</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation if needed</param>
    /// <returns>A task representing the asynchronous domain event dispatch operation that completes when all events have been processed</returns>
    /// <remarks>
    /// This method implements the Outbox pattern by persisting domain events to an outbox table within
    /// the same transaction as the aggregate data changes. The events are then published asynchronously
    /// by a background process, ensuring reliable delivery even in case of temporary messaging infrastructure failures.
    /// The method guarantees that either both data changes and event publishing succeed, or both fail atomically.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when dbContext or aggregatesWithEvents is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when the DbContext does not have an active transaction</exception>
    Task DispatchEventsWithDbContextAsync(
        DbContext dbContext,
        IEnumerable<IAggregateRoot> aggregatesWithEvents,
        CancellationToken cancellationToken = default
    );
}
