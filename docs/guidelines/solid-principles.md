# SOLID Principles in .NET

## Overview

This document provides comprehensive guidelines for implementing SOLID principles in .NET development for the Bank System Microservices project. These principles form the foundation of good object-oriented design.

## Single Responsibility Principle (SRP)

A class should have only one reason to change.

### Implementation Examples

```csharp
// ✅ Good: Single responsibility classes
public class TransactionValidator
{
    public ValidationResult ValidateTransaction(Transaction transaction)
    {
        if (transaction == null)
            return ValidationResult.Failure("Transaction cannot be null");

        if (transaction.Amount <= 0)
            return ValidationResult.Failure("Amount must be positive");

        if (transaction.AccountId == Guid.Empty)
            return ValidationResult.Failure("Valid account ID is required");

        return ValidationResult.Success();
    }
}

public class TransactionNotificationService
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;

    public async Task NotifyTransactionCompletedAsync(Transaction transaction)
    {
        var customer = await GetCustomerAsync(transaction.AccountId);

        if (customer.PreferredNotification == NotificationType.Email)
            await _emailService.SendTransactionNotificationAsync(customer.Email, transaction);
        else
            await _smsService.SendTransactionNotificationAsync(customer.PhoneNumber, transaction);
    }
}

public class TransactionRepository
{
    private readonly BankDbContext _context;

    public async Task<Transaction> GetByIdAsync(Guid id)
    {
        return await _context.Transactions.FindAsync(id);
    }

    public async Task AddAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }
}

// ❌ Bad: Multiple responsibilities in one class
public class TransactionService
{
    public async Task ProcessTransaction(Transaction transaction)
    {
        // Validation responsibility
        if (transaction.Amount <= 0)
            throw new ArgumentException("Invalid amount");

        // Data access responsibility
        using var connection = new SqlConnection(connectionString);
        // ... database operations

        // Notification responsibility
        var emailClient = new SmtpClient();
        // ... email sending logic

        // Logging responsibility
        File.WriteAllText("log.txt", $"Transaction processed: {transaction.Id}");
    }
}
```

## Open/Closed Principle (OCP)

Software entities should be open for extension but closed for modification.

### Implementation Examples

```csharp
// ✅ Good: Open for extension, closed for modification
public abstract class PaymentProcessor
{
    public abstract Task<PaymentResult> ProcessAsync(PaymentRequest request);

    protected virtual void LogPayment(PaymentRequest request)
    {
        // Common logging logic
    }

    protected virtual ValidationResult ValidateRequest(PaymentRequest request)
    {
        if (request == null)
            return ValidationResult.Failure("Request cannot be null");

        if (request.Amount <= 0)
            return ValidationResult.Failure("Amount must be positive");

        return ValidationResult.Success();
    }
}

public class CreditCardProcessor : PaymentProcessor
{
    public override async Task<PaymentResult> ProcessAsync(PaymentRequest request)
    {
        var validation = ValidateRequest(request);
        if (!validation.IsSuccess)
            return PaymentResult.Failure(validation.Error);

        LogPayment(request);

        // Credit card specific processing
        var result = await ProcessCreditCardPayment(request);
        return result;
    }

    private async Task<PaymentResult> ProcessCreditCardPayment(PaymentRequest request)
    {
        // Credit card specific implementation
        return PaymentResult.Success();
    }
}

public class BankTransferProcessor : PaymentProcessor
{
    public override async Task<PaymentResult> ProcessAsync(PaymentRequest request)
    {
        var validation = ValidateRequest(request);
        if (!validation.IsSuccess)
            return PaymentResult.Failure(validation.Error);

        LogPayment(request);

        // Bank transfer specific processing
        var result = await ProcessBankTransfer(request);
        return result;
    }

    private async Task<PaymentResult> ProcessBankTransfer(PaymentRequest request)
    {
        // Bank transfer specific implementation
        return PaymentResult.Success();
    }
}

// Using the processors
public class PaymentService
{
    private readonly Dictionary<PaymentType, PaymentProcessor> _processors;

    public PaymentService(IEnumerable<PaymentProcessor> processors)
    {
        _processors = processors.ToDictionary(p => p.PaymentType, p => p);
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        if (_processors.TryGetValue(request.PaymentType, out var processor))
        {
            return await processor.ProcessAsync(request);
        }

        return PaymentResult.Failure("Unsupported payment type");
    }
}

// ❌ Bad: Requires modification for new payment types
public class BadPaymentService
{
    public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
    {
        switch (request.PaymentType)
        {
            case PaymentType.CreditCard:
                // Credit card logic
                break;
            case PaymentType.BankTransfer:
                // Bank transfer logic
                break;
            // Need to modify this class to add new payment types
            default:
                return PaymentResult.Failure("Unsupported payment type");
        }
    }
}
```

## Liskov Substitution Principle (LSP)

Derived classes must be substitutable for their base classes.

### Implementation Examples

```csharp
// ✅ Good: Proper substitution
public abstract class Account
{
    public virtual decimal Balance { get; protected set; }
    public virtual decimal OverdraftLimit { get; protected set; } = 0;

    public virtual Result Withdraw(decimal amount)
    {
        if (amount <= 0)
            return Result.Failure("Amount must be positive");

        var availableBalance = Balance + OverdraftLimit;
        if (amount > availableBalance)
            return Result.Failure("Insufficient funds");

        Balance -= amount;
        return Result.Success();
    }
}

public class CheckingAccount : Account
{
    public override decimal OverdraftLimit { get; protected set; } = 500;

    // Follows contract of base class
    public override Result Withdraw(decimal amount)
    {
        // Can add specific checking account logic
        var result = base.Withdraw(amount);

        if (result.IsSuccess && Balance < 0)
        {
            // Apply overdraft fee for checking accounts
            ApplyOverdraftFee();
        }

        return result;
    }

    private void ApplyOverdraftFee()
    {
        Balance -= 35; // Overdraft fee
    }
}

public class SavingsAccount : Account
{
    private int _withdrawalsThisMonth;
    private const int MaxMonthlyWithdrawals = 6;

    public override Result Withdraw(decimal amount)
    {
        if (_withdrawalsThisMonth >= MaxMonthlyWithdrawals)
            return Result.Failure("Monthly withdrawal limit exceeded");

        var result = base.Withdraw(amount);
        if (result.IsSuccess)
        {
            _withdrawalsThisMonth++;
        }

        return result;
    }
}

// Client code can use any Account type
public class AccountService
{
    public Result ProcessWithdrawal(Account account, decimal amount)
    {
        // Works with any Account implementation
        return account.Withdraw(amount);
    }
}

// ❌ Bad: Violates LSP
public class BadReadOnlyAccount : Account
{
    public override Result Withdraw(decimal amount)
    {
        // Violates expectation of base class
        throw new InvalidOperationException("Cannot withdraw from read-only account");
    }
}
```

## Interface Segregation Principle (ISP)

No client should be forced to depend on methods it doesn't use.

### Implementation Examples

```csharp
// ✅ Good: Segregated interfaces
public interface IAccountReader
{
    Task<Account> GetByIdAsync(Guid id);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);
}

public interface IAccountWriter
{
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task DeleteAsync(Guid id);
}

public interface IAccountRepository : IAccountReader, IAccountWriter
{
    // Combines both interfaces for full repository access
}

// Query-only services only depend on what they need
public class AccountQueryService
{
    private readonly IAccountReader _accountReader;

    public AccountQueryService(IAccountReader accountReader)
    {
        _accountReader = accountReader;
    }

    public async Task<AccountDto> GetAccountAsync(Guid id)
    {
        var account = await _accountReader.GetByIdAsync(id);
        return MapToDto(account);
    }
}

// Command services only depend on what they need
public class AccountCommandService
{
    private readonly IAccountWriter _accountWriter;

    public AccountCommandService(IAccountWriter accountWriter)
    {
        _accountWriter = accountWriter;
    }

    public async Task CreateAccountAsync(CreateAccountCommand command)
    {
        var account = Account.CreateNew(command.CustomerId, command.AccountType);
        await _accountWriter.AddAsync(account);
    }
}

// ❌ Bad: Fat interface forces unnecessary dependencies
public interface IBadAccountService
{
    // Read operations
    Task<Account> GetByIdAsync(Guid id);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);

    // Write operations
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task DeleteAsync(Guid id);

    // Reporting operations
    Task<byte[]> GenerateAccountReportAsync(Guid id);
    Task<AccountStatistics> GetStatisticsAsync();

    // Email operations
    Task SendAccountStatementAsync(Guid id);
    Task SendWelcomeEmailAsync(Guid id);
}

public class ReadOnlyAccountService
{
    // Forced to depend on methods it doesn't use
    private readonly IBadAccountService _accountService;

    public ReadOnlyAccountService(IBadAccountService accountService)
    {
        _accountService = accountService; // Has access to write/email methods it doesn't need
    }
}
```

## Dependency Inversion Principle (DIP)

High-level modules should not depend on low-level modules. Both should depend on abstractions.

### Implementation Examples

```csharp
// ✅ Good: Depending on abstractions
public interface ITransactionRepository
{
    Task<Transaction> GetByIdAsync(Guid id);
    Task AddAsync(Transaction transaction);
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId);
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent) where T : IDomainEvent;
}

public interface ILogger<T>
{
    void LogInformation(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
}

// High-level module depends on abstractions
public class TransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionRepository repository,
        IEventPublisher eventPublisher,
        ILogger<TransactionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Transaction>> CreateTransactionAsync(CreateTransactionCommand command)
    {
        _logger.LogInformation("Creating transaction for account {AccountId}", command.AccountId);

        var transaction = Transaction.Create(command.AccountId, command.Amount, command.Type);

        await _repository.AddAsync(transaction);

        var transactionCreatedEvent = new TransactionCreatedEvent(transaction.Id, transaction.AccountId);
        await _eventPublisher.PublishAsync(transactionCreatedEvent);

        _logger.LogInformation("Transaction {TransactionId} created successfully", transaction.Id);

        return Result<Transaction>.Success(transaction);
    }
}

// Low-level modules implement the abstractions
public class SqlTransactionRepository : ITransactionRepository
{
    private readonly BankDbContext _context;

    public SqlTransactionRepository(BankDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> GetByIdAsync(Guid id)
    {
        return await _context.Transactions.FindAsync(id);
    }

    public async Task AddAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId)
    {
        return await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .ToListAsync();
    }
}

public class ServiceBusEventPublisher : IEventPublisher
{
    private readonly ServiceBusClient _serviceBusClient;

    public async Task PublishAsync<T>(T domainEvent) where T : IDomainEvent
    {
        var message = JsonSerializer.Serialize(domainEvent);
        await _serviceBusClient.SendMessageAsync(new ServiceBusMessage(message));
    }
}

// ❌ Bad: High-level module depending on low-level modules
public class BadTransactionService
{
    private readonly BankDbContext _context; // Direct dependency on EF Core
    private readonly ServiceBusClient _serviceBus; // Direct dependency on Service Bus
    private readonly ILogger _logger;

    public BadTransactionService(BankDbContext context, ServiceBusClient serviceBus)
    {
        _context = context;
        _serviceBus = serviceBus;
    }

    public async Task CreateTransactionAsync(CreateTransactionCommand command)
    {
        // Directly using EF Core
        var transaction = new Transaction { /* ... */ };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Directly using Service Bus
        var message = JsonSerializer.Serialize(new TransactionCreatedEvent());
        await _serviceBus.SendMessageAsync(new ServiceBusMessage(message));
    }
}
```

## Dependency Injection Configuration

```csharp
// Program.cs - Proper DI registration following DIP
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Register high-level services
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register repository implementations
        services.AddScoped<ITransactionRepository, SqlTransactionRepository>();
        services.AddScoped<IAccountRepository, SqlAccountRepository>();
        services.AddScoped<ICustomerRepository, SqlCustomerRepository>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register infrastructure implementations
        services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<INotificationService, SignalRNotificationService>();

        // Register payment processors
        services.AddScoped<PaymentProcessor, CreditCardProcessor>();
        services.AddScoped<PaymentProcessor, BankTransferProcessor>();

        return services;
    }
}

// Usage in Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBusinessServices();
builder.Services.AddRepositories();
builder.Services.AddInfrastructure(builder.Configuration);
```

## Summary

1. **Single Responsibility Principle**: Each class should have only one reason to change
2. **Open/Closed Principle**: Open for extension, closed for modification
3. **Liskov Substitution Principle**: Derived classes must be substitutable for base classes
4. **Interface Segregation Principle**: No client should depend on methods it doesn't use
5. **Dependency Inversion Principle**: Depend on abstractions, not concretions

Following these principles leads to:

- **More maintainable code** that's easier to modify
- **Better testability** through dependency injection
- **Increased flexibility** for future changes
- **Reduced coupling** between components
- **Improved code reusability** across the system
