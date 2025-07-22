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
    private Account()
    { }

    /// <summary>
    /// Initializes a new instance of the Account class.
    /// </summary>
    /// <param name="accountNumber">The unique account number.</param>
    /// <param name="customerId">The customer ID who owns the account.</param>
    /// <param name="type">The type of account.</param>
    /// <param name="currency">The currency for the account.</param>
    /// <param name="createdBy">The user who created the account.</param>
    private Account(
        AccountNumber accountNumber,
        Guid customerId,
        AccountType type,
        Currency currency,
        string createdBy)
    {
        Guard.AgainstNullOrEmpty(accountNumber, "accountNumber");
        Guard.AgainstNullOrEmpty(currency, "currency");
        Guard.AgainstEmptyGuid(customerId, "customerId");
        Guard.AgainstNullOrEmpty(createdBy, "createdBy");

        Id = Guid.NewGuid();
        AccountNumber = accountNumber;
        CustomerId = customerId;
        Type = type;
        Balance = Money.Zero(currency);
        Status = AccountStatus.PendingActivation;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Creates a new account for a customer.
    /// </summary>
    /// <param name="customerId">The customer ID who will own the account.</param>
    /// <param name="type">The type of account to create.</param>
    /// <param name="currency">The currency for the account.</param>
    /// <param name="createdBy">The user who is creating the account.</param>
    /// <returns>A new account instance.</returns>
    public static Account CreateNew(
        Guid customerId,
        AccountType type,
        Currency currency,
        string createdBy)
    {
        var accountNumber = AccountNumber.Generate();
        var account = new Account(
            accountNumber,
            customerId,
            type,
            currency,
            createdBy);

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
    /// <param name="activatedBy">The user who is activating the account.</param>
    public Result Activate(string activatedBy)
    {
        Guard.AgainstNullOrEmpty(activatedBy, "activatedBy");

        if (Status == AccountStatus.Active)
            return Result.Failure("Account is already active");

        if (Status == AccountStatus.Closed)
            return Result.Failure("Cannot activate a closed account");

        Status = AccountStatus.Active;
        UpdatedBy = activatedBy;

        AddDomainEvent(new AccountActivatedEvent(Id, CustomerId, AccountNumber.Value, DateTime.UtcNow));
        return Result.Success();
    }

    /// <summary>
    /// Suspends the account, preventing new transactions.
    /// </summary>
    /// <param name="reason">The reason for suspension.</param>
    /// <param name="suspendedBy">The user who is suspending the account.</param>
    public Result Suspend(string reason, string suspendedBy)
    {
        Guard.AgainstNullOrEmpty(reason, "reason");
        Guard.AgainstNullOrEmpty(suspendedBy, "suspendedBy");

        if (Status == AccountStatus.Closed)
            return Result.Failure("Cannot suspend a closed account");

        Status = AccountStatus.Suspended;
        UpdatedBy = suspendedBy;

        AddDomainEvent(new AccountSuspendedEvent(Id, reason, DateTime.UtcNow, suspendedBy));
        return Result.Success();
    }

    /// <summary>
    /// Freezes the account.
    /// </summary>
    /// <param name="reason">The reason for freezing the account.</param>
    /// <param name="freezeBy">The user who is freezing the account.</param>
    public Result Freeze(string reason, string freezeBy)
    {
        Guard.AgainstNullOrEmpty(reason, "reason");
        Guard.AgainstNullOrEmpty(freezeBy, "freezeBy");

        if (Status == AccountStatus.Frozen)
            return Result.Failure("Account is already frozen");

        if (Status == AccountStatus.Closed)
            return Result.Failure("Cannot freeze a closed account");

        Status = AccountStatus.Frozen;
        UpdatedBy = freezeBy;

        AddDomainEvent(new AccountFrozenEvent(Id, AccountNumber, CustomerId, reason));
        return Result.Success();
    }

    /// <summary>
    /// Closes the account permanently.
    /// </summary>
    /// <param name="reason">The reason for closing the account.</param>
    /// <param name="closedBy">The user who is closing the account.</param>
    public Result Close(string reason, string closedBy)
    {
        Guard.AgainstNullOrEmpty(reason, "reason");
        Guard.AgainstNullOrEmpty(closedBy, "closedBy");

        if (Status == AccountStatus.Closed)
            return Result.Failure("Account is already closed");

        if (!Balance.IsZero)
            return Result.Failure("Cannot close account with non-zero balance");

        Status = AccountStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdatedBy = closedBy;

        AddDomainEvent(new AccountClosedEvent(Id, AccountNumber, CustomerId, reason, Balance));
        return Result.Success();
    }
}