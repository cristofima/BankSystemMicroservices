namespace BankSystem.Shared.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain invariant is violated.
/// </summary>
public class DomainException(string message) : Exception(message)
{
}
