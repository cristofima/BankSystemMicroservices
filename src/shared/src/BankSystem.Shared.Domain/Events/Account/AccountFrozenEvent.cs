using BankSystem.Shared.Domain.ValueObjects;

namespace BankSystem.Shared.Domain.Events.Account;

public record AccountFrozenEvent(
    Guid AccountId,
    AccountNumber AccountNumber,
    Guid CustomerId,
    string Reason
) : DomainEvent { }
