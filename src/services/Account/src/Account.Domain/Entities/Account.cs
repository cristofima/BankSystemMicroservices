using BankSystem.Account.Domain.Enums;
using BankSystem.Account.Domain.Events;
using BankSystem.Account.Domain.ValueObjects;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
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
    public Money Balance { get; } = null!;

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
    private Account() { }

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
        Currency currency
    )
    {
        Guard.AgainstNullOrEmpty(accountNumber);
        Guard.AgainstNullOrEmpty(currency);
        Guard.AgainstEmptyGuid(customerId);

        Id = Guid.NewGuid();
        AccountNumber = accountNumber;
        CustomerId = customerId;
        Type = type;
        Balance = Money.Zero(currency);
        Status = AccountStatus.PendingActivation;
    }

    /// <summary>
    /// Creates a new account for a customer.
    /// </summary>
    /// <param name="customerId">The customer ID who will own the account.</param>
    /// <param name="type">The type of account to create.</param>
    /// <param name="currency">The currency for the account.</param>
    /// <returns>A new account instance.</returns>
    public static Account CreateNew(Guid customerId, AccountType type, Currency currency)
    {
        var accountNumber = AccountNumber.Generate();
        var account = new Account(accountNumber, customerId, type, currency);

        account.AddDomainEvent(
            new AccountCreatedEvent(
                account.Id,
                customerId,
                account.AccountNumber,
                type.ToString(),
                DateTime.UtcNow
            )
        );

        return account;
    }

    /// <summary>
    /// Activates the account, allowing transactions to be processed.
    /// </summary>
    public Result Activate()
    {
        if (Status == AccountStatus.Active)
            return Result.Failure("Account is already active");

        if (Status == AccountStatus.Closed)
            return Result.Failure("Cannot activate a closed account");

        Status = AccountStatus.Active;

        AddDomainEvent(
            new AccountActivatedEvent(Id, CustomerId, AccountNumber.Value, DateTime.UtcNow)
        );
        return Result.Success();
    }

    /// <summary>
    /// Suspends the account, preventing new transactions.
    /// </summary>
    /// <param name="reason">The reason for suspension.</param>
    public Result Suspend(string reason)
    {
        Guard.AgainstNullOrEmpty(reason, "reason");

        if (Status == AccountStatus.Closed)
            return Result.Failure("Cannot suspend a closed account");

        Status = AccountStatus.Suspended;

        AddDomainEvent(new AccountSuspendedEvent(Id, reason, DateTime.UtcNow));
        return Result.Success();
    }

    /// <summary>
    /// Freezes the account.
    /// </summary>
    /// <param name="reason">The reason for freezing the account.</param>
    public Result Freeze(string reason)
    {
        Guard.AgainstNullOrEmpty(reason);

        if (Status == AccountStatus.Frozen)
            return Result.Failure("Account is already frozen");

        if (Status == AccountStatus.Closed)
            return Result.Failure("Cannot freeze a closed account");

        Status = AccountStatus.Frozen;

        AddDomainEvent(new AccountFrozenEvent(Id, AccountNumber, CustomerId, reason));
        return Result.Success();
    }

    /// <summary>
    /// Closes the account permanently.
    /// </summary>
    /// <param name="reason">The reason for closing the account.</param>
    public Result Close(string reason)
    {
        Guard.AgainstNullOrEmpty(reason);

        if (Status == AccountStatus.Closed)
            return Result.Failure("Account is already closed");

        if (!Balance.IsZero)
            return Result.Failure("Cannot close account with non-zero balance");

        Status = AccountStatus.Closed;
        ClosedAt = DateTime.UtcNow;

        AddDomainEvent(new AccountClosedEvent(Id, AccountNumber, CustomerId, reason));
        return Result.Success();
    }
}
