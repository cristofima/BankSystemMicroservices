namespace BankSystem.Shared.Domain.Events.Account;

/// <summary>
/// Domain event raised when an account is activated.
/// </summary>
public record AccountActivatedEvent(
    Guid AccountId,
    Guid CustomerId,
    string AccountNumber,
    DateTimeOffset ActivatedAt
) : DomainEvent { }
