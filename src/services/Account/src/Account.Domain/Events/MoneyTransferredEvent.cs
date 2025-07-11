using BankSystem.Shared.Domain.Common;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when money is transferred between accounts.
/// </summary>
public record MoneyTransferredEvent(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string Currency,
    string Description,
    string TransferReference) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
