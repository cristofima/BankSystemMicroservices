using BankSystem.Shared.Kernel.Events;

namespace BankSystem.Shared.Domain.Events;

/// <summary>
/// Base implementation for domain events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public int Version => 1;
}
