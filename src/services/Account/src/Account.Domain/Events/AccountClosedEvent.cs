using Account.Domain.ValueObjects;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.ValueObjects;

namespace Account.Domain.Events;

/// <summary>
/// Domain event raised when an account is closed.
/// </summary>
public record AccountClosedEvent(
    Guid AccountId,
    AccountNumber AccountNumber,
    Guid CustomerId,
    string Reason,
    Money FinalBalance) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
