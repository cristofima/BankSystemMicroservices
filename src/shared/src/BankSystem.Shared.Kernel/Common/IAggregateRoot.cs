using BankSystem.Shared.Kernel.Events;

namespace BankSystem.Shared.Kernel.Common;

/// <summary>
/// Marker interface for aggregate roots that can produce domain events.
/// This interface provides a common contract for accessing domain events
/// from any aggregate root without knowing its specific generic type.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Domain events produced by this aggregate.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Adds a domain event to the aggregate.
    /// </summary>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    void ClearDomainEvents();
}
