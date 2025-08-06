using BankSystem.Shared.Domain.Common;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when an account is suspended.
/// </summary>
public record AccountSuspendedEvent(Guid AccountId, string Reason, DateTime SuspendedAt)
    : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
