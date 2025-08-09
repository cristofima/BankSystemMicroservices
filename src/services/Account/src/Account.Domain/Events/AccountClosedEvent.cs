using BankSystem.Account.Domain.ValueObjects;
using BankSystem.Shared.Domain.Events;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when an account is closed.
/// </summary>
public record AccountClosedEvent(
    Guid AccountId,
    AccountNumber AccountNumber,
    Guid CustomerId,
    string Reason
) : DomainEvent { }
