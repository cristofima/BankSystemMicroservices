using BankSystem.Shared.Kernel.Events;

namespace BankSystem.Shared.Infrastructure.UnitTests.Common;

public record TestDomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public int Version { get; init; } = 1;
}
