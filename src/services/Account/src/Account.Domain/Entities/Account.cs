using BankSystem.Account.Domain.Enums;
using BankSystem.Account.Domain.Events;
using BankSystem.Account.Domain.Exceptions;
using BankSystem.Account.Domain.ValueObjects;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Exceptions;
using BankSystem.Shared.Domain.ValueObjects;

namespace BankSystem.Account.Domain.Entities;

/// <summary>
/// Represents a bank account aggregate root in the banking domain.
/// An account is the primary entity for managing customer finances and transactions.
/// </summary>
public class Account : AggregateRoot<Guid>
{

    /// <summary>
    /// Gets the unique account number for this account.
    /// </summary>
    public AccountNumber AccountNumber { get; } = null!;

    /// <summary>
    /// Gets the customer who owns this account.
    /// </summary>
    public Guid CustomerId { get; }

    /// <summary>
    /// Gets the current balance of the account.
    /// </summary>
    public Money Balance { get; private set; } = null!;

    /// <summary>
    /// Gets the current status of the account.
    /// </summary>
    public AccountStatus Status { get; private set; }

    /// <summary>
    /// Gets the type of account (Checking, Savings, etc.).
    /// </summary>
    public AccountType Type { get; private set; }

    /// <summary>
    /// Gets the date and time when the account was closed (if applicable).
    /// </summary>
    public DateTime? ClosedAt { get; private set; }

    // Private constructor for EF Core
    private Account()
    { }

    /// <summary>
    /// Initializes a new instance of the Account class.
    /// </summary>
    /// <param name="accountNumber">The unique account number.</param>
    /// <param name="customerId">The customer ID who owns the account.</param>
    /// <param name="type">The type of account.</param>
    /// <param name="currency">The currency for the account.</param>
    private Account(
        AccountNumber accountNumber,
        Guid customerId,
        AccountType type,
        Currency currency)
    {
        Id = Guid.NewGuid();
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        CustomerId = customerId == Guid.Empty ? throw new ArgumentException("Customer ID cannot be empty", nameof(customerId)) : customerId;
        Type = type;
        Balance = Money.Zero(currency);
        Status = AccountStatus.PendingActivation;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new account for a customer.
    /// </summary>
    /// <param name="customerId">The customer ID who will own the account.</param>
    /// <param name="type">The type of account to create.</param>
    /// <param name="currency">The currency for the account.</param>
    /// <returns>A new account instance.</returns>
    public static Account CreateNew(
        Guid customerId,
        AccountType type,
        Currency currency)
    {
        var accountNumber = AccountNumber.Generate();
        var account = new Account(
            accountNumber,
            customerId,
            type,
            currency);

        account.AddDomainEvent(new AccountCreatedEvent(
            account.Id,
            customerId,
            account.AccountNumber,
            type.ToString(),
            DateTime.UtcNow));

        return account;
    }

    /// <summary>
    /// Activates the account, allowing transactions to be processed.
    /// </summary>
    public void Activate()
    {
        switch (Status)
        {
            case AccountStatus.Active:
                throw new DomainException("Account is already active");
            case AccountStatus.Closed:
                throw new DomainException("Cannot activate a closed account");
            case AccountStatus.Suspended:
            case AccountStatus.PendingActivation:
            case AccountStatus.Frozen:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Status = AccountStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AccountActivatedEvent(Id, CustomerId, AccountNumber.Value, DateTime.UtcNow));
    }

    /// <summary>
    /// Suspends the account, preventing new transactions.
    /// </summary>
    /// <param name="reason">The reason for suspension.</param>
    public void Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Suspension reason is required", nameof(reason));

        if (Status == AccountStatus.Closed)
            throw new DomainException("Cannot suspend a closed account");

        Status = AccountStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AccountSuspendedEvent(Id, reason, DateTime.UtcNow, "System"));
    }

    /// <summary>
    /// Closes the account permanently.
    /// </summary>
    /// <param name="reason">The reason for closing the account.</param>
    public Result Close(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Closure reason is required");

        if (Status == AccountStatus.Closed)
            return Result.Failure("Account is already closed");

        if (!Balance.IsZero)
            return Result.Failure("Cannot close account with non-zero balance");

        Status = AccountStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AccountClosedEvent(Id, AccountNumber, CustomerId, reason, Balance));
        return Result.Success();
    }

    /// <summary>
    /// Deposits money into the account.
    /// </summary>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="description">Description of the deposit.</param>
    public Result Deposit(Money amount, string description)
    {
        ArgumentNullException.ThrowIfNull(amount);
        
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("Description is required");
        if (Status != AccountStatus.Active)
            return Result.Failure("Cannot deposit to inactive account");
        if (!amount.IsPositive)
            return Result.Failure("Deposit amount must be positive");

        Balance = Balance.Add(amount);
        AddDomainEvent(new AccountCreditedEvent(Id, amount.Amount, amount.Currency.ToString(), Balance.Amount, description));
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Withdraws money from the account.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="description">Description of the withdrawal.</param>
    public Result Withdraw(Money amount, string description)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (amount.Amount <= 0)
            return Result.Failure("Withdrawal amount must be positive");

        if (!amount.Currency.Equals(Balance.Currency))
            return Result.Failure("Withdrawal currency must match account currency");

        if (Status != AccountStatus.Active)
            return Result.Failure("Cannot withdraw from inactive account");

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("Transaction description is required");

        // Check if withdrawal is allowed (balance cannot go below zero)
        if (Balance.IsLessThan(amount))
            throw new InsufficientFundsException(Id, amount.Amount, Balance.Amount);

        Balance = Balance.Subtract(amount);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AccountDebitedEvent(
            Id,
            amount,
            Balance,
            description));

        return Result.Success();
    }
}