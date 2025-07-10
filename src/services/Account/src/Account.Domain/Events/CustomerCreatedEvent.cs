using BankSystem.Shared.Domain.Common;

namespace Account.Domain.Events;

/// <summary>
/// Domain event raised when a new customer is created.
/// </summary>
public record CustomerCreatedEvent(
    Guid CustomerId,
    string FirstName,
    string LastName,
    string Email,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
