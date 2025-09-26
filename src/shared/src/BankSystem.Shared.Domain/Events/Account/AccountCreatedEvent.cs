using BankSystem.Shared.Domain.ValueObjects;

namespace BankSystem.Shared.Domain.Events.Account;

/// <summary>
/// Domain event raised when a new account is created.
/// </summary>
public record AccountCreatedEvent(
    Guid AccountId,
    Guid CustomerId,
    AccountNumber AccountNumber,
    string AccountType,
    DateTimeOffset CreatedAt
) : DomainEvent { }
