using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Events;

namespace BankSystem.Shared.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        Guard.AgainstNull(domainEvent, nameof(domainEvent));
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
