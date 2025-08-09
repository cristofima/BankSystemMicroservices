using BankSystem.Account.Domain.ValueObjects;
using BankSystem.Shared.Domain.Events;

namespace BankSystem.Account.Domain.Events;

public record AccountFrozenEvent(
    Guid AccountId,
    AccountNumber AccountNumber,
    Guid CustomerId,
    string Reason
) : DomainEvent { }
