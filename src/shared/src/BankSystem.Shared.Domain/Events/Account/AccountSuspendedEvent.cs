namespace BankSystem.Shared.Domain.Events.Account;

/// <summary>
/// Domain event raised when an account is suspended.
/// </summary>
public record AccountSuspendedEvent(Guid AccountId, string Reason, DateTimeOffset SuspendedAt)
    : DomainEvent { }
