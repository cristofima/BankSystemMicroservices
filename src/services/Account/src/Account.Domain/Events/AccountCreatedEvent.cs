using BankSystem.Account.Domain.ValueObjects;
using BankSystem.Shared.Domain.Common;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when a new account is created.
/// </summary>
public record AccountCreatedEvent(
    Guid AccountId,
    Guid CustomerId,
    AccountNumber AccountNumber,
    string AccountType,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
