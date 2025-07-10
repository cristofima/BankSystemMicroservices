namespace Account.Domain.Enums;

/// <summary>
/// Represents the type of transaction.
/// </summary>
public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Transfer = 3,
    Fee = 4,
    Interest = 5,
    Refund = 6,
    Adjustment = 7
}
