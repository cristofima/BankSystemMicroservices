using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Kernel.Events;

namespace BankSystem.Shared.Infrastructure.UnitTests.Common;

public class TestEntity : AggregateRoot<Guid>
{
    public TestEntity()
    {
        Id = Guid.NewGuid();
    }

    public new void AddDomainEvent(IDomainEvent domainEvent)
    {
        base.AddDomainEvent(domainEvent);
    }
}
