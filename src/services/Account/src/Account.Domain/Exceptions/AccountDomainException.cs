namespace Account.Domain.Exceptions;

/// <summary>
/// Base exception for all account domain-related business rule violations.
/// </summary>
public class AccountDomainException : Exception
{
    public AccountDomainException(string message) : base(message)
    {
    }

    public AccountDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
