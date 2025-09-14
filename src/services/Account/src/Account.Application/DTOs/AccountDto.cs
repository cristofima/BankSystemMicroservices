namespace BankSystem.Account.Application.DTOs;

/// <summary>
/// Data transfer object representing an account
/// </summary>
public record AccountDto
{
    /// <summary>
    /// The unique identifier of the account
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The account number
    /// </summary>
    public string AccountNumber { get; init; } = string.Empty;

    /// <summary>
    /// The unique identifier of the customer who owns the account
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// The type of account (Checking, Savings, Business)
    /// </summary>
    public string AccountType { get; init; } = string.Empty;

    /// <summary>
    /// The current balance of the account
    /// </summary>
    public decimal Balance { get; init; }

    /// <summary>
    /// The currency of the account balance
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// The current status of the account
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// The date and time when the account was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The date and time when the account was last updated
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}
