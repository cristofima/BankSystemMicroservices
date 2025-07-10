namespace Account.Domain.Exceptions;

/// <summary>
/// Exception thrown when an account has insufficient funds for a requested operation.
/// </summary>
public class InsufficientFundsException : AccountDomainException
{
    public decimal RequestedAmount { get; }
    public decimal AvailableBalance { get; }
    public Guid AccountId { get; }

    public InsufficientFundsException(Guid accountId, decimal requestedAmount, decimal availableBalance)
        : base($"Insufficient funds for account {accountId}. Requested: {requestedAmount:C}, Available: {availableBalance:C}")
    {
        AccountId = accountId;
        RequestedAmount = requestedAmount;
        AvailableBalance = availableBalance;
    }

    public InsufficientFundsException(Guid accountId, decimal requestedAmount, decimal availableBalance, string message)
        : base(message)
    {
        AccountId = accountId;
        RequestedAmount = requestedAmount;
        AvailableBalance = availableBalance;
    }
}
