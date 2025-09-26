using BankSystem.Shared.Domain.ValueObjects;

namespace BankSystem.Shared.Domain.Events.Account;

/// <summary>
/// Domain event raised when an account is closed.
/// </summary>
public record AccountClosedEvent(
    Guid AccountId,
    AccountNumber AccountNumber,
    Guid CustomerId,
    string Reason
) : DomainEvent { }
