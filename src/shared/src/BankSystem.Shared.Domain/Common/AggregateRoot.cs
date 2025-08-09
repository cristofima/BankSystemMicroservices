using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Events;

namespace BankSystem.Shared.Domain.Common;

/// <summary>
/// Marker base class for aggregate roots that can produce domain events.
/// This class provides a common contract for accessing domain events
/// from any aggregate root without knowing its specific generic type.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events produced by this aggregate.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate.
    /// </summary>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
