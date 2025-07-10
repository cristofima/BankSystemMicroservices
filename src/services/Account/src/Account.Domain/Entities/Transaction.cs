using Account.Domain.Enums;
using Account.Domain.Guards;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.ValueObjects;

namespace Account.Domain.Entities;

/// <summary>
/// Represents a financial transaction within the banking system.
/// This entity captures all transaction details including amount, type, and metadata.
/// </summary>
public class Transaction : Entity<Guid>
{
    private Transaction()
    { } // Required for EF Core

    /// <summary>
    /// Gets the unique identifier of the account this transaction belongs to.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Gets the transaction amount and currency.
    /// </summary>
    public Money Amount { get; private init; } = null!;

    /// <summary>
    /// Gets the type of transaction (Deposit, Withdrawal, Transfer, etc.).
    /// </summary>
    public TransactionType Type { get; private init; }

    /// <summary>
    /// Gets the description or memo for this transaction.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the reference number for this transaction (e.g., check number, wire reference).
    /// </summary>
    public string? ReferenceNumber { get; private set; }

    /// <summary>
    /// Gets the current status of the transaction.
    /// </summary>
    public TransactionStatus Status { get; private set; }

    /// <summary>
    /// Gets the date and time when the transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the transaction was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Gets the running balance after this transaction was applied.
    /// </summary>
    public Money? BalanceAfter { get; private set; }

    /// <summary>
    /// Gets additional metadata about the transaction (channel, location, etc.).
    /// </summary>
    public TransactionMetadata? Metadata { get; private set; }

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="amount">The transaction amount</param>
    /// <param name="type">The transaction type</param>
    /// <param name="description">The transaction description</param>
    /// <param name="referenceNumber">Optional reference number</param>
    /// <param name="metadata">Optional transaction metadata</param>
    /// <returns>A new transaction instance</returns>
    public static Transaction Create(
        Guid accountId,
        Money amount,
        TransactionType type,
        string description,
        string? referenceNumber = null,
        TransactionMetadata? metadata = null)
    {
        Guard.AgainstEmptyGuid(accountId, nameof(accountId));
        Guard.AgainstNull(amount, nameof(amount));
        Guard.AgainstNullOrWhiteSpace(description, nameof(description));

        if (description.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters", nameof(description));

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Amount = amount,
            Type = type,
            Description = description.Trim(),
            ReferenceNumber = referenceNumber?.Trim(),
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Metadata = metadata
        };

        return transaction;
    }

    /// <summary>
    /// Processes the transaction, setting it to completed status.
    /// </summary>
    /// <param name="balanceAfter">The account balance after this transaction</param>
    public void Process(Money balanceAfter)
    {
        Guard.AgainstNull(balanceAfter, nameof(balanceAfter));

        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot process transaction in {Status} status");

        if (balanceAfter.Currency != Amount.Currency)
            throw new ArgumentException("Balance currency must match transaction currency");

        Status = TransactionStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        BalanceAfter = balanceAfter;
    }

    /// <summary>
    /// Cancels the transaction.
    /// </summary>
    /// <param name="reason">The reason for cancellation</param>
    public void Cancel(string reason)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        switch (Status)
        {
            case TransactionStatus.Completed:
                throw new InvalidOperationException("Cannot cancel a completed transaction");
            case TransactionStatus.Cancelled:
                throw new InvalidOperationException("Transaction is already cancelled");
            case TransactionStatus.Pending:
            case TransactionStatus.Failed:
            default:
                Status = TransactionStatus.Cancelled;

                // Update metadata with cancellation reason
                Metadata = Metadata?.WithCancellationReason(reason) ??
                           TransactionMetadata.Create().WithCancellationReason(reason);
                break;
        }
    }

    /// <summary>
    /// Fails the transaction due to an error.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    public void Fail(string errorMessage)
    {
        Guard.AgainstNullOrWhiteSpace(errorMessage, nameof(errorMessage));

        if (Status == TransactionStatus.Completed)
            throw new InvalidOperationException("Cannot fail a completed transaction");

        Status = TransactionStatus.Failed;

        // Update metadata with error information
        Metadata = Metadata?.WithError(errorMessage) ??
                  TransactionMetadata.Create().WithError(errorMessage);
    }

    /// <summary>
    /// Checks if the transaction is a debit (reduces account balance).
    /// </summary>
    public bool IsDebit() => Type == TransactionType.Withdrawal ||
                            Type == TransactionType.Transfer ||
                            Type == TransactionType.Fee;

    /// <summary>
    /// Checks if the transaction is a credit (increases account balance).
    /// </summary>
    public bool IsCredit() => Type is TransactionType.Deposit or TransactionType.Interest or TransactionType.Refund;

    /// <summary>
    /// Gets the effective amount for balance calculation (negative for debits).
    /// </summary>
    public Money GetEffectiveAmount()
    {
        return IsDebit() ? new Money(-Amount.Amount, Amount.Currency) : Amount;
    }

    /// <summary>
    /// Creates a deposit transaction.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="amount">The deposit amount</param>
    /// <param name="description">The transaction description</param>
    /// <returns>A new deposit transaction instance</returns>
    public static Transaction CreateDeposit(Guid accountId, Money amount, string description)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID cannot be empty", nameof(accountId));

        if (amount is null || amount.Amount <= 0)
            throw new ArgumentException("Deposit amount must be positive", nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Amount = amount,
            Type = TransactionType.Deposit,
            Description = description.Trim(),
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a withdrawal transaction.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="amount">The withdrawal amount</param>
    /// <param name="description">The transaction description</param>
    /// <returns>A new withdrawal transaction instance</returns>
    public static Transaction CreateWithdrawal(Guid accountId, Money amount, string description)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID cannot be empty", nameof(accountId));

        if (amount is null || amount.Amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive", nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Amount = amount,
            Type = TransactionType.Withdrawal,
            Description = description.Trim(),
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
}