namespace BankSystem.Account.Domain.Enums;

/// <summary>
/// Represents the status of a transaction.
/// </summary>
public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}