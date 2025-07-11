using BankSystem.Account.Domain.Entities;
using BankSystem.Account.Domain.Enums;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.ValueObjects;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when a new transaction is created.
/// </summary>
public record TransactionCreatedEvent(
    Guid TransactionId,
    Guid AccountId,
    Money Amount,
    TransactionType Type,
    string Description,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
