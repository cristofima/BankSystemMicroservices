using BankSystem.Shared.Domain.Events;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when an account is activated.
/// </summary>
public record AccountActivatedEvent(
    Guid AccountId,
    Guid CustomerId,
    string AccountNumber,
    DateTime ActivatedAt
) : DomainEvent { }
