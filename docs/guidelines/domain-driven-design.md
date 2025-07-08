# Domain-Driven Design (DDD) Guidelines

## Overview

Domain-Driven Design (DDD) is a software development approach that focuses on understanding and modeling the business domain. This guideline provides patterns and practices for implementing DDD in the Bank System Microservices project.

## Core DDD Concepts

### Entities

Entities have identity and their state can change over time. They represent core business objects.

```csharp
public class Account : AggregateRoot<Guid>
{
    private readonly List<Transaction> _transactions = new();

    public string AccountNumber { get; private set; }
    public Money Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    public Guid CustomerId { get; private set; }

    // Domain behavior encapsulated in entity
    public Result Withdraw(Money amount, string description)
    {
        // Guard clauses first
        if (amount.Amount <= 0)
            return Result.Failure("Amount must be positive");

        if (Status != AccountStatus.Active)
            return Result.Failure("Account is not active");

        if (Balance.Amount < amount.Amount)
            return Result.Failure("Insufficient funds");

        // Business logic
        Balance = Balance.Subtract(amount);
        var transaction = Transaction.CreateWithdrawal(Id, amount, description);
        _transactions.Add(transaction);

        // Domain event
        AddDomainEvent(new MoneyWithdrawnEvent(Id, amount, Balance));

        return Result.Success();
    }

    // Factory method for creation
    public static Account CreateNew(string accountNumber, Guid customerId, Money initialDeposit)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            CustomerId = customerId,
            Balance = initialDeposit,
            Status = AccountStatus.Active
        };

        account.AddDomainEvent(new AccountCreatedEvent(account.Id, accountNumber, customerId));
        return account;
    }
}
```

### Value Objects

Value objects have no identity and are defined by their attributes. They should be immutable.

```csharp
public record Money(decimal Amount, Currency Currency)
{
    public static Money Zero(Currency currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} to {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    // Validation in constructor
    public Money
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
    }
}

public record EmailAddress(string Value)
{
    public EmailAddress
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email address cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email address format", nameof(value));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static implicit operator string(EmailAddress email) => email.Value;
    public static explicit operator EmailAddress(string email) => new(email);
}
```

### Aggregate Roots

Aggregate roots are entities that serve as the entry point to an aggregate. They maintain consistency boundaries.

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> entity)
            return false;

        if (ReferenceEquals(this, entity))
            return true;

        if (GetType() != entity.GetType())
            return false;

        return !EqualityComparer<TId>.Default.Equals(Id, default) &&
               EqualityComparer<TId>.Default.Equals(Id, entity.Id);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }
}
```

### Domain Events

Domain events represent something important that happened in the domain.

```csharp
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}

public record AccountCreatedEvent(
    Guid AccountId,
    string AccountNumber,
    Guid CustomerId) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MoneyWithdrawnEvent(
    Guid AccountId,
    Money Amount,
    Money NewBalance) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TransactionCreatedEvent(
    Guid TransactionId,
    Guid AccountId,
    Money Amount,
    TransactionType Type,
    string Description) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

### Domain Services

Domain services contain domain logic that doesn't naturally fit within an entity or value object.

```csharp
public interface ITransferDomainService
{
    Task<Result> TransferMoneyAsync(
        Account fromAccount,
        Account toAccount,
        Money amount,
        string description,
        CancellationToken cancellationToken = default);
}

public class TransferDomainService : ITransferDomainService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<TransferDomainService> _logger;

    public TransferDomainService(
        IAccountRepository accountRepository,
        ILogger<TransferDomainService> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<Result> TransferMoneyAsync(
        Account fromAccount,
        Account toAccount,
        Money amount,
        string description,
        CancellationToken cancellationToken = default)
    {
        // Business rules validation
        if (fromAccount.CustomerId == toAccount.CustomerId)
            return Result.Failure("Cannot transfer to the same customer");

        if (amount.Amount <= 0)
            return Result.Failure("Transfer amount must be positive");

        // Execute domain operations
        var withdrawResult = fromAccount.Withdraw(amount, $"Transfer to {toAccount.AccountNumber}: {description}");
        if (!withdrawResult.IsSuccess)
            return withdrawResult;

        var depositResult = toAccount.Deposit(amount, $"Transfer from {fromAccount.AccountNumber}: {description}");
        if (!depositResult.IsSuccess)
        {
            // Compensate the withdrawal
            fromAccount.Deposit(amount, "Transfer reversal");
            return depositResult;
        }

        // Add transfer domain event
        var transferEvent = new MoneyTransferredEvent(
            fromAccount.Id,
            toAccount.Id,
            amount,
            description);

        fromAccount.AddDomainEvent(transferEvent);

        _logger.LogInformation("Money transfer completed: {Amount} from {FromAccount} to {ToAccount}",
            amount.Amount, fromAccount.AccountNumber, toAccount.AccountNumber);

        return Result.Success();
    }
}
```

## DDD Patterns Implementation

### Repository Pattern for Aggregates

```csharp
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task DeleteAsync(Account account, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### Specification Pattern

```csharp
public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T entity);

    public Specification<T> And(Specification<T> other)
    {
        return new AndSpecification<T>(this, other);
    }

    public Specification<T> Or(Specification<T> other)
    {
        return new OrSpecification<T>(this, other);
    }

    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}

public class ActiveAccountSpecification : Specification<Account>
{
    public override bool IsSatisfiedBy(Account account)
    {
        return account.Status == AccountStatus.Active;
    }
}

public class SufficientBalanceSpecification : Specification<Account>
{
    private readonly Money _requiredAmount;

    public SufficientBalanceSpecification(Money requiredAmount)
    {
        _requiredAmount = requiredAmount;
    }

    public override bool IsSatisfiedBy(Account account)
    {
        return account.Balance.Amount >= _requiredAmount.Amount &&
               account.Balance.Currency == _requiredAmount.Currency;
    }
}
```

### Factory Pattern

```csharp
public interface IAccountFactory
{
    Account CreateCheckingAccount(Guid customerId, Money initialDeposit);
    Account CreateSavingsAccount(Guid customerId, Money initialDeposit);
    Account CreateBusinessAccount(Guid customerId, Money initialDeposit);
}

public class AccountFactory : IAccountFactory
{
    private readonly IAccountNumberGenerator _accountNumberGenerator;

    public AccountFactory(IAccountNumberGenerator accountNumberGenerator)
    {
        _accountNumberGenerator = accountNumberGenerator;
    }

    public Account CreateCheckingAccount(Guid customerId, Money initialDeposit)
    {
        var accountNumber = _accountNumberGenerator.GenerateCheckingAccountNumber();
        return Account.CreateNew(accountNumber, customerId, initialDeposit, AccountType.Checking);
    }

    public Account CreateSavingsAccount(Guid customerId, Money initialDeposit)
    {
        var accountNumber = _accountNumberGenerator.GenerateSavingsAccountNumber();
        return Account.CreateNew(accountNumber, customerId, initialDeposit, AccountType.Savings);
    }

    public Account CreateBusinessAccount(Guid customerId, Money initialDeposit)
    {
        var accountNumber = _accountNumberGenerator.GenerateBusinessAccountNumber();
        return Account.CreateNew(accountNumber, customerId, initialDeposit, AccountType.Business);
    }
}
```

## Best Practices

### Entity Design

1. **Encapsulate Business Logic**: Keep business rules within entities
2. **Use Factory Methods**: Create entities through static factory methods
3. **Avoid Anemic Models**: Entities should have behavior, not just data
4. **Guard Clauses**: Validate inputs at the beginning of methods
5. **Domain Events**: Publish events for important business occurrences

### Value Object Design

1. **Immutability**: Value objects should be immutable
2. **Validation**: Validate in constructor and throw exceptions for invalid state
3. **Equality**: Implement equality based on all properties
4. **Self-Validation**: Value objects should validate themselves
5. **Use Records**: Leverage C# records for concise value object implementation

### Aggregate Design

1. **Consistency Boundary**: Aggregates define consistency boundaries
2. **Small Aggregates**: Keep aggregates small for better performance
3. **Reference by ID**: Reference other aggregates by ID, not direct reference
4. **Single Root**: One aggregate root per aggregate
5. **Transaction Boundary**: One transaction per aggregate modification

### Domain Service Guidelines

1. **Stateless**: Domain services should be stateless
2. **Single Responsibility**: Each service should have one clear responsibility
3. **Domain Logic Only**: Keep infrastructure concerns out of domain services
4. **Testable**: Design for easy unit testing
5. **Interface Segregation**: Use focused interfaces

---

_This guideline follows the principles outlined in [Clean Code Guidelines](./clean-code.md) and [SOLID Principles](./solid-principles.md)._
