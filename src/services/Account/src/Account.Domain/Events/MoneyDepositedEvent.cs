using BankSystem.Shared.Domain.Common;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when money is deposited into an account.
/// </summary>
public record MoneyDepositedEvent(
    Guid AccountId,
    decimal Amount,
    string Currency,
    decimal NewBalance,
    string Description) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
