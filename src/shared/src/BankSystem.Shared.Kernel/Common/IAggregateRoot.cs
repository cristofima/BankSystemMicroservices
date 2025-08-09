using BankSystem.Shared.Kernel.Events;

namespace BankSystem.Shared.Kernel.Common;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}
