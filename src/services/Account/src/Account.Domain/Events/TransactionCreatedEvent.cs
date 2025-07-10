using Account.Domain.Entities;
using Account.Domain.Enums;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.ValueObjects;

namespace Account.Domain.Events;

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
