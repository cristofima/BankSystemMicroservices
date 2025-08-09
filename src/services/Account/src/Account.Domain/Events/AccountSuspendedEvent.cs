using BankSystem.Shared.Domain.Events;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when an account is suspended.
/// </summary>
public record AccountSuspendedEvent(Guid AccountId, string Reason, DateTime SuspendedAt)
    : DomainEvent { }
