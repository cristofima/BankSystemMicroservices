namespace BankSystem.Shared.Domain.Common;

/// <summary>
/// Represents a domain event within the Domain-Driven Design context.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// The date and time when the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}
