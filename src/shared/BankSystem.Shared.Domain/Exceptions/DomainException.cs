namespace BankSystem.Shared.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain invariant is violated.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
