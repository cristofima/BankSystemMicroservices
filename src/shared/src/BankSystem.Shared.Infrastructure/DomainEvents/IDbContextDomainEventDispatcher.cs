using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BankSystem.Shared.Infrastructure.DomainEvents;

/// <summary>
/// Extended domain event dispatcher interface that can work with DbContext for Outbox pattern
/// </summary>
public interface IDbContextDomainEventDispatcher : IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches events from aggregates within a DbContext transaction for Outbox pattern
    /// </summary>
    /// <param name="dbContext">The DbContext to use for event publishing</param>
    /// <param name="aggregatesWithEvents">Aggregates that have domain events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task DispatchEventsWithDbContextAsync(
        DbContext dbContext,
        IEnumerable<IAggregateRoot> aggregatesWithEvents,
        CancellationToken cancellationToken = default
    );
}
