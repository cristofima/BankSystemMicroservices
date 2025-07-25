using BankSystem.Account.Domain.ValueObjects;
using BankSystem.Shared.Domain.Common;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when an account is closed.
/// </summary>
public record AccountClosedEvent(
    Guid AccountId,
    AccountNumber AccountNumber,
    Guid CustomerId,
    string Reason) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
