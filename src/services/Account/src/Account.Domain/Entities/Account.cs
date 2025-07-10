using Account.Domain.Enums;
using Account.Domain.Events;
using Account.Domain.Exceptions;
using Account.Domain.ValueObjects;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.ValueObjects;

namespace Account.Domain.Entities;

/// <summary>
/// Represents a bank account aggregate root in the banking domain.
/// An account is the primary entity for managing customer finances and transactions.
/// </summary>
public class Account : AggregateRoot<Guid>
{
    private readonly List<Transaction> _transactions = [];

    /// <summary>
    /// Gets the unique account number for this account.
    /// </summary>
    public AccountNumber AccountNumber { get; private set; } = null!;

    /// <summary>
    /// Gets the customer who owns this account.
    /// </summary>
    public Guid CustomerId { get; private set; }

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
    /// Gets the date and time when the account was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the account was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the account was closed (if applicable).
    /// </summary>
    public DateTime? ClosedAt { get; private set; }

    /// <summary>
    /// Gets the read-only collection of transactions for this account.
    /// </summary>
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    /// <summary>
    /// Gets a value indicating whether the account is active.
    /// </summary>
    public bool IsActive => Status == AccountStatus.Active;

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
    /// <param name="initialDeposit">The initial deposit amount (optional).</param>
    /// <returns>A new account instance.</returns>
    public static Account CreateNew(
        Guid customerId,
        AccountType type,
        Currency currency,
        Money? initialDeposit = null)
    {
        var accountNumber = AccountNumber.Generate();
        var account = new Account(
            accountNumber,
            customerId,
            type,
            currency);

        initialDeposit ??= Money.Zero(currency);

        // Set initial deposit directly, even if PendingActivation
        if (initialDeposit?.Amount > 0)
        {
            account.Balance = initialDeposit;
            var initialDepositTransaction = Transaction.CreateDeposit(
                account.Id,
                initialDeposit,
                "Initial deposit");
            account._transactions.Add(initialDepositTransaction);
            account.AddDomainEvent(new MoneyDepositedEvent(
                account.Id,
                initialDeposit.Amount,
                currency,
                account.Balance.Amount,
                "Initial deposit"));
        }

        account.AddDomainEvent(new AccountCreatedEvent(
            account.Id,
            customerId,
            account.AccountNumber,
            initialDeposit,
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
                throw new AccountDomainException("Account is already active");
            case AccountStatus.Closed:
                throw new AccountDomainException("Cannot activate a closed account");
            case AccountStatus.Suspended:
            case AccountStatus.PendingActivation:
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
            throw new AccountDomainException("Cannot suspend a closed account");

        Status = AccountStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AccountSuspendedEvent(Id, reason, DateTime.UtcNow, "System"));
    }

    /// <summary>
    /// Closes the account permanently.
    /// </summary>
    /// <param name="reason">The reason for closing the account.</param>
    public void Close(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Closure reason is required", nameof(reason));

        if (Status == AccountStatus.Closed)
            throw new AccountDomainException("Account is already closed");

        if (Balance.Amount != 0)
            throw new AccountDomainException("Cannot close account with non-zero balance");

        Status = AccountStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AccountClosedEvent(Id, AccountNumber, CustomerId, reason, Balance));
    }

    /// <summary>
    /// Deposits money into the account.
    /// </summary>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="description">Description of the deposit.</param>
    /// <returns>The created transaction.</returns>
    public Result Deposit(Money amount, string description)
    {
        if (amount is null)
            throw new ArgumentNullException(nameof(amount));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));
        if (Status != AccountStatus.Active)
            throw new AccountDomainException("Cannot deposit to inactive account");
        if (amount.Amount <= 0)
            return Result.Failure("Deposit amount must be positive");

        Balance = Balance.Add(amount);
        var transaction = Transaction.CreateDeposit(Id, amount, description);
        _transactions.Add(transaction);
        AddDomainEvent(new MoneyDepositedEvent(Id, amount.Amount, amount.Currency.ToString(), Balance.Amount, description));
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Withdraws money from the account.
    /// </summary>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="description">Description of the withdrawal.</param>
    /// <returns>The created transaction.</returns>
    public Transaction Withdraw(Money amount, string description)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (amount.Amount <= 0)
            throw new AccountDomainException("Withdrawal amount must be positive");

        if (!amount.Currency.Equals(Balance.Currency))
            throw new AccountDomainException("Withdrawal currency must match account currency");

        if (Status != AccountStatus.Active)
            throw new AccountDomainException("Cannot withdraw from inactive account");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Transaction description is required", nameof(description));

        // Check if withdrawal is allowed (balance cannot go below zero)
        if (Balance.Amount < amount.Amount)
            throw new InsufficientFundsException(Id, amount.Amount, Balance.Amount);

        Balance = Balance.Subtract(amount);
        UpdatedAt = DateTime.UtcNow;

        var transaction = Transaction.CreateWithdrawal(Id, amount, description);
        _transactions.Add(transaction);

        AddDomainEvent(new MoneyWithdrawnEvent(
            Id,
            amount,
            Balance,
            description,
            DateTime.UtcNow));

        return transaction;
    }




}