using BankSystem.Shared.Domain.Common;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when an account is activated.
/// </summary>
public record AccountActivatedEvent(
    Guid AccountId,
    Guid CustomerId,
    string AccountNumber,
    DateTime ActivatedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
