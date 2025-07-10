using BankSystem.Shared.Domain.Common;

namespace Account.Domain.Events;

/// <summary>
/// Domain event raised when an account is deactivated.
/// </summary>
public record AccountDeactivatedEvent(
    Guid AccountId,
    Guid CustomerId,
    string AccountNumber,
    DateTime DeactivatedAt,
    string Reason) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
