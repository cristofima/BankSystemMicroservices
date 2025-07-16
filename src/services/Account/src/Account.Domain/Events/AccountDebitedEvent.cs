using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.ValueObjects;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when money is withdrawn from an account.
/// </summary>
public record AccountDebitedEvent(
    Guid AccountId,
    Money Amount,
    Money NewBalance,
    string Description) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
