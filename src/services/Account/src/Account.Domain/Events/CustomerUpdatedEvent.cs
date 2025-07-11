using BankSystem.Shared.Domain.Common;

namespace BankSystem.Account.Domain.Events;

/// <summary>
/// Domain event raised when customer information is updated.
/// </summary>
public record CustomerUpdatedEvent(
    Guid CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateTime UpdatedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
