using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Events;

namespace BankSystem.Shared.Infrastructure.UnitTests.Common;

public class TestAggregateRoot : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
