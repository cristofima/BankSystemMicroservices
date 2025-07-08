# .NET 9 Code Generation Guidelines

## Overview

This document provides comprehensive guidelines for generating high-quality .NET 9 code in the Bank System Microservices project. This is the master reference that consolidates all coding standards and directs you to specific, focused guidelines for different aspects of development.

**Important**: All code generation should follow the [Clean Code principles](./clean-code.md) outlined in our Clean Code guidelines. This document complements those principles with specific .NET implementation patterns.

## Related Guidelines

This document is part of a comprehensive set of coding guidelines. For specific topics, refer to these detailed guides:

### Core Principles and Patterns

- **[Clean Code Guidelines](./clean-code.md)** - Fundamental principles for readable, maintainable code
- **[SOLID Principles](./solid-principles.md)** - Object-oriented design principles implementation
- **[Naming Conventions](./naming-conventions.md)** - Comprehensive naming standards and code organization
- **[Documentation Standards](./documentation-standards.md)** - XML documentation, comments, and API documentation

### Architecture and Design Patterns

- **[Domain-Driven Design](./domain-driven-design.md)** - DDD patterns and practices
- **[CQRS Implementation](./cqrs-implementation.md)** - Command Query Responsibility Segregation patterns
- **[Error Handling](./error-handling.md)** - Result patterns and exception handling strategies
- **[Repository Pattern](./repository-pattern.md)** - Data access abstraction patterns

### Technology-Specific Guidelines

- **[ASP.NET Core Best Practices](./aspnetcore-best-practices.md)** - Performance, security, and maintainability practices
- **[API Design](./api-design.md)** - RESTful API design and security practices
- **[Entity Framework Core](./entity-framework-core.md)** - EF Core migrations, DbContext, and performance guidelines
- **[Async/Await Best Practices](./async-await-best-practices.md)** - Asynchronous programming guidelines
- **[Configuration Management](./configuration-management.md)** - IOptions pattern and configuration best practices

### Performance and Quality

- **[Performance Guidelines](./performance-guidelines.md)** - Memory management, database optimization, and scalability
- **[Static Methods Best Practices](./static-methods-best-practices.md)** - CA1822 guidance and static method patterns

## Prerequisites

Before generating any code, ensure you understand and apply:

1. **Clean Code Fundamentals** - Code should be readable, focused, and well-tested
2. **Meaningful Names** - Use intention-revealing, searchable names (see [Naming Conventions](./naming-conventions.md))
3. **Small Functions** - Functions should be small and do one thing
4. **Error Handling** - Use Result patterns and appropriate exceptions (see [Error Handling](./error-handling.md))
5. **SOLID Principles** - Especially Single Responsibility and Dependency Inversion (see [SOLID Principles](./solid-principles.md))

Refer to the specific guideline documents for detailed principles and examples.

## Quick Reference

This section provides essential patterns and examples. For comprehensive coverage, see the specific guideline documents.

### Essential Naming Conventions

**For complete naming guidelines, see [Naming Conventions](./naming-conventions.md)**

- **Classes**: Use PascalCase (e.g., `TransactionService`, `AccountController`)
- **Methods**: Use PascalCase (e.g., `ProcessTransactionAsync`, `GetAccountByIdAsync`)
- **Properties**: Use PascalCase (e.g., `AccountId`, `TransactionAmount`)
- **Fields**: Use camelCase with underscore prefix for private fields (e.g., `_accountRepository`, `_logger`)
- **Constants**: Use PascalCase (e.g., `MaxTransactionAmount`)
- **Local Variables**: Use camelCase (e.g., `transactionId`, `accountBalance`)
- **Parameters**: Use camelCase (e.g., `accountId`, `amount`)

### Basic File Organization

```csharp
// File header with namespace
namespace BankSystem.Transaction.Application.Commands;

// Using statements - organize by: System, Microsoft, Third-party, Local
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FluentValidation;
using MediatR;
using BankSystem.Transaction.Domain.Entities;

// Class declaration with proper spacing
public class CreateTransactionCommand : IRequest<TransactionDto>
{
    // Properties first
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }

    // Constructor if needed
    public CreateTransactionCommand(Guid accountId, decimal amount)
    {
        AccountId = accountId;
        Amount = amount;
    }
}
```

## Implementation Patterns

For detailed implementation patterns, refer to the specialized guidelines:

### Domain Logic

See [Domain-Driven Design Guidelines](./domain-driven-design.md) for:

- Entity patterns and aggregate roots
- Value objects and domain services
- Domain events and business rules

### Application Layer

See [CQRS Implementation](./cqrs-implementation.md) for:

- Command and query patterns
- Handler implementation
- Validation strategies

### Data Access

See [Repository Pattern](./repository-pattern.md) and [Entity Framework Core](./entity-framework-core.md) for:

- Repository abstraction patterns
- Entity Framework best practices
- Database performance optimization

### API Development

See [API Design Guidelines](./api-design.md) and [ASP.NET Core Best Practices](./aspnetcore-best-practices.md) for:

- RESTful API implementation
- Controller patterns and security
- Performance optimization

### Error Handling

See [Error Handling Guidelines](./error-handling.md) for:

- Result patterns and exception strategies
- Validation patterns
- Error response formatting

### Asynchronous Programming

See [Async/Await Best Practices](./async-await-best-practices.md) for:

- Proper async/await usage
- Cancellation token patterns
- Parallel processing strategies

### Performance Optimization

See [Performance Guidelines](./performance-guidelines.md) for:

- Memory management best practices
- Database optimization strategies
- Caching patterns

### Static Methods

See [Static Methods Best Practices](./static-methods-best-practices.md) for:

- CA1822 analyzer guidance
- Factory method patterns
- Utility function design

### Configuration

See [Configuration Management](./configuration-management.md) for:

- IOptions pattern implementation
- Environment-specific configuration
- Secrets management

### Documentation

See [Documentation Standards](./documentation-standards.md) for:

- XML documentation guidelines
- Code comment best practices
- API documentation standards

## Core Patterns Reference

This section provides essential patterns. For comprehensive examples and detailed explanations, refer to the specific guideline documents.

### Domain Entity Example

**For complete patterns, see [Domain-Driven Design](./domain-driven-design.md)**

```csharp
public class Account : AggregateRoot<Guid>
{
    private readonly List<Transaction> _transactions = new();

    public string AccountNumber { get; private set; }
    public Money Balance { get; private set; }
    public AccountStatus Status { get; private set; }

    // Domain behavior encapsulated in entity
    public Result Withdraw(Money amount, string description)
    {
        // Guard clauses and business logic
        if (Status != AccountStatus.Active)
            return Result.Failure("Account is not active");

        if (Balance.Amount < amount.Amount)
            return Result.Failure("Insufficient funds");

        // Update state and raise domain events
        Balance = Balance.Subtract(amount);
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

### Value Object Example

**For complete patterns, see [Domain-Driven Design](./domain-driven-design.md)**

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

    // Validation in constructor
    public Money
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
    }
}
```

### Command Handler Example

**For complete patterns, see [CQRS Implementation](./cqrs-implementation.md)**

```csharp
public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Result<TransactionDto>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public async Task<Result<TransactionDto>> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // Validation, domain logic, persistence, and event publishing
        // See CQRS Implementation guide for complete example
    }
}
```

### Repository Example

**For complete patterns, see [Repository Pattern](./repository-pattern.md)**

```csharp
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
}

public class AccountRepository : IAccountRepository
{
    private readonly BankDbContext _context;
    private readonly ILogger<AccountRepository> _logger;

    // Implementation with proper error handling and logging
    // See Repository Pattern guide for complete example
}
```

### API Controller Example

**For complete patterns, see [API Design](./api-design.md) and [ASP.NET Core Best Practices](./aspnetcore-best-practices.md)**

```csharp
[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount(
        [FromBody] CreateAccountCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAccount), new { accountId = result.Value.Id }, result.Value)
            : BadRequest(result.Error);
    }

    // See API Design guide for complete examples
}
```

````

## CQRS Implementation

### Command Pattern

```csharp
// Command - immutable record
public record CreateDepositCommand(
    Guid AccountId,
    decimal Amount,
    string Currency,
    string Description,
    string Reference) : IRequest<Result<TransactionDto>>;

// Command Validator
public class CreateDepositCommandValidator : AbstractValidator<CreateDepositCommand>
{
    public CreateDepositCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be positive")
            .LessThanOrEqualTo(50000)
            .WithMessage("Amount cannot exceed daily limit");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");
    }
}

// Command Handler
public class CreateDepositCommandHandler : IRequestHandler<CreateDepositCommand, Result<TransactionDto>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateDepositCommandHandler> _logger;

    public CreateDepositCommandHandler(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IEventPublisher eventPublisher,
        IMapper mapper,
        ILogger<CreateDepositCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<TransactionDto>> Handle(
        CreateDepositCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve account
            var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Account {AccountId} not found", request.AccountId);
                return Result<TransactionDto>.Failure("Account not found");
            }

            // Create money value object
            var amount = new Money(request.Amount, Currency.FromCode(request.Currency));

            // Execute domain logic
            var depositResult = account.Deposit(amount, request.Description);
            if (!depositResult.IsSuccess)
            {
                _logger.LogWarning("Deposit failed for account {AccountId}: {Error}",
                    request.AccountId, depositResult.Error);
                return Result<TransactionDto>.Failure(depositResult.Error);
            }

            // Persist changes
            await _accountRepository.UpdateAsync(account, cancellationToken);

            // Get the created transaction
            var transaction = account.Transactions.Last();
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            // Publish domain events
            foreach (var domainEvent in account.DomainEvents)
            {
                await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
            account.ClearDomainEvents();

            // Map to DTO
            var transactionDto = _mapper.Map<TransactionDto>(transaction);

            _logger.LogInformation("Deposit of {Amount} {Currency} processed for account {AccountId}",
                request.Amount, request.Currency, request.AccountId);

            return Result<TransactionDto>.Success(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit for account {AccountId}", request.AccountId);
            return Result<TransactionDto>.Failure("An error occurred while processing the deposit");
        }
    }
}
````

### Query Pattern

```csharp
// Query - immutable record
public record GetAccountTransactionsQuery(
    Guid AccountId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<Result<PagedList<TransactionDto>>>;

// Query Handler
public class GetAccountTransactionsQueryHandler
    : IRequestHandler<GetAccountTransactionsQuery, Result<PagedList<TransactionDto>>>
{
    private readonly ITransactionQueryRepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public async Task<Result<PagedList<TransactionDto>>> Handle(
        GetAccountTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        // Create cache key
        var cacheKey = $"transactions_{request.AccountId}_{request.FromDate}_{request.ToDate}_{request.PageNumber}_{request.PageSize}";

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out PagedList<TransactionDto> cachedResult))
        {
            return Result<PagedList<TransactionDto>>.Success(cachedResult);
        }

        // Query from repository
        var transactions = await _repository.GetPagedByAccountIdAsync(
            request.AccountId,
            request.FromDate,
            request.ToDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var result = _mapper.Map<PagedList<TransactionDto>>(transactions);

        // Cache for 5 minutes
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return Result<PagedList<TransactionDto>>.Success(result);
    }
}
```

## Error Handling

### Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public string Error { get; }

    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static Result<T> Failure(string error) => new(false, default, error);
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
}
```

### Custom Exceptions

```csharp
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(Guid accountId)
        : base($"Account with ID '{accountId}' was not found") { }
}

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(decimal requestedAmount, decimal availableBalance)
        : base($"Insufficient funds. Requested: {requestedAmount}, Available: {availableBalance}") { }
}
```

## Repository Pattern

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

public class AccountRepository : IAccountRepository
{
    private readonly BankDbContext _context;
    private readonly ILogger<AccountRepository> _logger;

    public AccountRepository(BankDbContext context, ILogger<AccountRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Accounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account {AccountId}", id);
            throw;
        }
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account {AccountId} added successfully", account.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding account {AccountId}", account.Id);
            throw;
        }
    }
}
```

## Async/Await Best Practices

```csharp
// ✅ Good: Proper async/await usage
public async Task<Result<Account>> CreateAccountAsync(CreateAccountCommand command)
{
    // Use ConfigureAwait(false) in libraries
    var existingAccount = await _repository.GetByAccountNumberAsync(
        command.AccountNumber).ConfigureAwait(false);

    if (existingAccount != null)
        return Result<Account>.Failure("Account number already exists");

    var account = Account.CreateNew(command.AccountNumber, command.CustomerId);
    await _repository.AddAsync(account).ConfigureAwait(false);

    return Result<Account>.Success(account);
}

// ❌ Bad: Blocking async calls
public Account CreateAccount(CreateAccountCommand command)
{
    var result = CreateAccountAsync(command).Result; // Don't do this!
    return result.Value;
}

// ✅ Good: Parallel async operations
public async Task<Result> ProcessMultipleTransactionsAsync(List<TransactionCommand> commands)
{
    var tasks = commands.Select(cmd => ProcessTransactionAsync(cmd));
    var results = await Task.WhenAll(tasks);

    return results.All(r => r.IsSuccess)
        ? Result.Success()
        : Result.Failure("One or more transactions failed");
}
```

## Performance Guidelines

```csharp
// ✅ Use StringBuilder for multiple string operations
public string BuildTransactionSummary(IEnumerable<Transaction> transactions)
{
    var sb = new StringBuilder();
    foreach (var transaction in transactions)
    {
        sb.AppendLine($"{transaction.Date:yyyy-MM-dd}: {transaction.Amount:C}");
    }
    return sb.ToString();
}

// ✅ Use LINQ efficiently
public async Task<IEnumerable<Account>> GetActiveAccountsWithHighBalanceAsync()
{
    return await _context.Accounts
        .Where(a => a.Status == AccountStatus.Active)
        .Where(a => a.Balance.Amount > 10000)
        .OrderByDescending(a => a.Balance.Amount)
        .Take(100)
        .ToListAsync();
}

// ✅ Use cancellation tokens
public async Task<Result> ProcessLongRunningOperationAsync(CancellationToken cancellationToken)
{
    for (int i = 0; i < 1000; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await ProcessItemAsync(i, cancellationToken);
    }

    return Result.Success();
}
```

## Code Comments and Documentation

```csharp
/// <summary>
/// Processes a withdrawal transaction for the specified account.
/// </summary>
/// <param name="accountId">The unique identifier of the account</param>
/// <param name="amount">The amount to withdraw</param>
/// <param name="description">A description of the withdrawal</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>A result containing the transaction details if successful</returns>
/// <exception cref="AccountNotFoundException">Thrown when the account is not found</exception>
/// <exception cref="InsufficientFundsException">Thrown when there are insufficient funds</exception>
public async Task<Result<TransactionDto>> ProcessWithdrawalAsync(
    Guid accountId,
    decimal amount,
    string description,
    CancellationToken cancellationToken = default)
{
    // Guard clauses - validate input parameters
    if (accountId == Guid.Empty)
        return Result<TransactionDto>.Failure("Account ID cannot be empty");

    if (amount <= 0)
        return Result<TransactionDto>.Failure("Amount must be positive");

    // Business logic with clear intent
    var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
    if (account == null)
        throw new AccountNotFoundException(accountId);

    // Complex business rule - explain with comment
    // Apply daily withdrawal limit check (regulatory requirement)
    var dailyWithdrawals = await GetDailyWithdrawalsAsync(accountId, cancellationToken);
    if (dailyWithdrawals + amount > DailyWithdrawalLimit)
        return Result<TransactionDto>.Failure("Daily withdrawal limit exceeded");

    // Execute domain operation
    var withdrawalResult = account.Withdraw(new Money(amount, Currency.USD), description);
    if (!withdrawalResult.IsSuccess)
        return Result<TransactionDto>.Failure(withdrawalResult.Error);

    // Persist and publish events
    await _accountRepository.UpdateAsync(account, cancellationToken);
    await PublishDomainEventsAsync(account.DomainEvents, cancellationToken);

    return Result<TransactionDto>.Success(_mapper.Map<TransactionDto>(withdrawalResult.Value));
}
```

## ASP.NET Core Best Practices

### Avoid Blocking Calls

ASP.NET Core apps should process many requests simultaneously. Avoid blocking calls that could be asynchronous:

```csharp
// ✅ Good: Asynchronous operations
public class TransactionController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

// ❌ Bad: Blocking async calls
public class BadTransactionController : ControllerBase
{
    [HttpPost]
    public ActionResult<TransactionDto> CreateTransaction(CreateTransactionCommand command)
    {
        var result = _mediator.Send(command).Result; // Don't do this!
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
```

### Return Large Collections with Pagination

Don't return large collections all at once. Implement pagination:

```csharp
// ✅ Good: Paginated results
[HttpGet]
public async Task<ActionResult<PagedResult<TransactionDto>>> GetTransactions(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    CancellationToken cancellationToken = default)
{
    pageSize = Math.Min(pageSize, 100); // Limit maximum page size

    var query = new GetTransactionsQuery(page, pageSize);
    var result = await _mediator.Send(query, cancellationToken);

    return Ok(result);
}

// ✅ Good: Use IAsyncEnumerable for streaming
public async IAsyncEnumerable<TransactionDto> GetTransactionsStream(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var transaction in _repository.GetTransactionsStreamAsync(cancellationToken))
    {
        yield return _mapper.Map<TransactionDto>(transaction);
    }
}
```

### Optimize Data Access and I/O

Make all data access operations asynchronous and efficient:

```csharp
// ✅ Good: Optimized data access
public class AccountService
{
    private readonly IAccountRepository _repository;
    private readonly IMemoryCache _cache;

    public async Task<Account> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        // Try cache first
        var cacheKey = $"account_{accountId}";
        if (_cache.TryGetValue(cacheKey, out Account cachedAccount))
        {
            return cachedAccount;
        }

        // Use no-tracking query for read-only data
        var account = await _repository.GetByIdNoTrackingAsync(accountId, cancellationToken);

        // Cache for 5 minutes
        _cache.Set(cacheKey, account, TimeSpan.FromMinutes(5));

        return account;
    }
}

// ✅ Good: Minimize network round trips
public async Task<AccountWithTransactionsDto> GetAccountWithRecentTransactionsAsync(
    Guid accountId,
    CancellationToken cancellationToken)
{
    // Single query instead of multiple calls
    var accountWithTransactions = await _context.Accounts
        .Include(a => a.Transactions.OrderByDescending(t => t.CreatedAt).Take(10))
        .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

    return _mapper.Map<AccountWithTransactionsDto>(accountWithTransactions);
}
```

### Use HttpClientFactory for HTTP Connections

Pool HTTP connections properly:

```csharp
// ✅ Good: Use HttpClientFactory
public class ExternalPaymentService
{
    private readonly HttpClient _httpClient;

    public ExternalPaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/payments", request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PaymentResult>();
    }
}

// Register in Program.cs
builder.Services.AddHttpClient<ExternalPaymentService>(client =>
{
    client.BaseAddress = new Uri("https://api.paymentprovider.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### Avoid Large Object Allocations

Minimize allocations in hot code paths:

```csharp
// ✅ Good: Use ArrayPool for large arrays
public class CsvReportGenerator
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    public async Task<byte[]> GenerateReportAsync(IEnumerable<Transaction> transactions)
    {
        const int bufferSize = 1024 * 1024; // 1MB
        var buffer = _arrayPool.Rent(bufferSize);

        try
        {
            using var stream = new MemoryStream(buffer);
            await WriteCsvDataAsync(stream, transactions);
            return stream.ToArray();
        }
        finally
        {
            _arrayPool.Return(buffer);
        }
    }
}

// ✅ Good: Use StringBuilder for string concatenation
public string FormatTransactionSummary(IEnumerable<Transaction> transactions)
{
    var sb = new StringBuilder();
    foreach (var transaction in transactions)
    {
        sb.AppendLine($"{transaction.Date:yyyy-MM-dd}: {transaction.Amount:C} - {transaction.Description}");
    }
    return sb.ToString();
}
```

### Handle HttpContext Properly

Never store HttpContext in fields or access it from multiple threads:

```csharp
// ✅ Good: Pass data explicitly
[HttpPost]
public async Task<ActionResult> ProcessTransactionAsync(
    CreateTransactionCommand command)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

    // Pass explicit parameters instead of HttpContext
    var result = await _transactionService.ProcessTransactionAsync(
        command, userId, ipAddress);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
}

// ❌ Bad: Storing HttpContext
public class BadService
{
    private readonly HttpContext _context; // Don't do this!

    public BadService(IHttpContextAccessor accessor)
    {
        _context = accessor.HttpContext; // Don't store HttpContext
    }
}
```

### Implement Proper Error Handling

Don't expose internal details in error responses:

```csharp
// ✅ Good: Secure error handling
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException => new {
                Type = "validation_error",
                Title = "Validation Failed",
                Status = 400,
                Detail = "One or more validation errors occurred"
            },
            DomainException => new {
                Type = "business_error",
                Title = "Business Rule Violation",
                Status = 400,
                Detail = exception.Message
            },
            _ => new {
                Type = "internal_error",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "An unexpected error occurred"
            }
        };

        context.Response.StatusCode = response.Status;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### Use Background Services for Long-Running Tasks

Don't block HTTP requests with long-running operations:

```csharp
// ✅ Good: Background service for long-running tasks
public class TransactionProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionProcessingService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var transactionQueue = scope.ServiceProvider.GetRequiredService<ITransactionQueue>();

            var pendingTransactions = await transactionQueue.DequeueBatchAsync(10, stoppingToken);

            var tasks = pendingTransactions.Select(ProcessTransactionAsync);
            await Task.WhenAll(tasks);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

// ✅ Good: Controller queues work and returns immediately
[HttpPost("bulk")]
public async Task<ActionResult> QueueBulkTransactions(
    [FromBody] IEnumerable<CreateTransactionCommand> commands)
{
    var jobId = Guid.NewGuid();
    await _transactionQueue.EnqueueBulkAsync(jobId, commands);

    return Accepted(new { JobId = jobId, Status = "Queued" });
}
```

## Static Methods and CA1822 Best Practices

### Understanding CA1822 Analyzer Rule

The CA1822 analyzer rule flags methods that don't access instance data and suggests marking them as static. This improves performance and makes the code intent clearer.

### When to Use Static Methods

```csharp
// ✅ Good: Static methods for utility functions
public class TokenResponseFactory
{
    // Static method - doesn't need instance data
    public static TokenResponse CreateTokenResponse(string accessToken, string refreshToken, int expiresIn)
    {
        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            TokenType = "Bearer"
        };
    }

    // Static method for validation
    public static bool IsValidTokenFormat(string token)
    {
        return !string.IsNullOrEmpty(token) && token.Length >= 32;
    }
}

// ✅ Good: Static factory methods in domain entities
public class Account
{
    private readonly List<Transaction> _transactions = new();

    // Instance method - uses instance data
    public Result Withdraw(Money amount, string description)
    {
        // Uses instance fields
        if (Balance.Amount < amount.Amount)
            return Result.Failure("Insufficient funds");

        // Implementation...
    }

    // Static factory method - creates new instances
    public static Account CreateNew(string accountNumber, Guid customerId, Money initialDeposit)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            CustomerId = customerId,
            Balance = initialDeposit,
            Status = AccountStatus.Active
        };
    }

    // Static validation method
    public static bool IsValidAccountNumber(string accountNumber)
    {
        return !string.IsNullOrEmpty(accountNumber) &&
               accountNumber.Length == 10 &&
               accountNumber.All(char.IsDigit);
    }
}
```

### Response/DTO Factory Patterns

```csharp
// ✅ Good: Static factory methods for DTOs
public static class ResponseFactory
{
    public static ApiResponse<T> Success<T>(T data, string message = "Operation successful")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Failure<T>(string error, IEnumerable<string>? additionalErrors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = error,
            Errors = additionalErrors ?? Enumerable.Empty<string>(),
            Timestamp = DateTime.UtcNow
        };
    }

    public static PagedResult<T> CreatePagedResult<T>(
        IEnumerable<T> data,
        int currentPage,
        int pageSize,
        int totalRecords)
    {
        return new PagedResult<T>
        {
            Data = data,
            Pagination = new PaginationInfo
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                HasNextPage = currentPage * pageSize < totalRecords,
                HasPreviousPage = currentPage > 1
            }
        };
    }
}
```

### Validation Helper Methods

```csharp
// ✅ Good: Static validation helpers
public static class ValidationHelpers
{
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

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

    public static bool IsValidCurrency(string currencyCode)
    {
        return !string.IsNullOrEmpty(currencyCode) &&
               currencyCode.Length == 3 &&
               CurrencyCodes.IsValid(currencyCode);
    }

    public static bool IsValidAmount(decimal amount)
    {
        return amount > 0 && amount <= 1_000_000m;
    }
}
```

### Mapping and Conversion Utilities

```csharp
// ✅ Good: Static mapping methods
public static class EntityMappers
{
    public static UserResponse ToUserResponse(this ApplicationUser user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public static TransactionDto ToTransactionDto(this Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        };
    }

    // Static conversion methods
    public static decimal ToDecimal(this Money money)
    {
        return money.Amount;
    }

    public static Money ToMoney(this decimal amount, string currencyCode = "USD")
    {
        return new Money(amount, Currency.FromCode(currencyCode));
    }
}
```

### Constants and Configuration Helpers

```csharp
// ✅ Good: Static constants and helpers
public static class SecurityConstants
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 100;
    public const int DefaultTokenExpiryMinutes = 60;
    public const int MaxFailedLoginAttempts = 5;

    public static TimeSpan GetLockoutDuration() => TimeSpan.FromMinutes(15);

    public static bool IsPasswordCompliant(string password)
    {
        return !string.IsNullOrEmpty(password) &&
               password.Length >= MinPasswordLength &&
               password.Length <= MaxPasswordLength &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => !char.IsLetterOrDigit(c));
    }
}
```

### When NOT to Use Static Methods

```csharp
// ❌ Bad: Don't make methods static if they might need instance data later
public class EmailService
{
    private readonly IEmailConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    // Don't make this static - it uses injected dependencies
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Sending email to {Email}", to);
        // Uses _config for SMTP settings
        // Implementation...
    }
}

// ❌ Bad: Don't make repository methods static
public class UserRepository
{
    private readonly DbContext _context;

    // Don't make this static - it uses DbContext
    public async Task<User> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }
}
```

### Addressing CA1822 in Code Reviews

When the CA1822 analyzer flags a method:

1. **Evaluate if the method should be static**:

   - Does it use any instance fields or properties?
   - Will it likely need instance data in the future?
   - Is it a utility/helper method?

2. **Consider the design implications**:

   - Static methods are harder to mock in tests
   - They can make classes less cohesive if overused
   - They're appropriate for pure functions and utilities

3. **Suppression when appropriate**:
   ```csharp
   // Suppress CA1822 if method might need instance data in future
   [SuppressMessage("Performance", "CA1822:Mark members as static",
       Justification = "Method may require instance data in future iterations")]
   public string GetDeviceInfo()
   {
       // Method implementation that doesn't currently use instance data
       // but may need to access configuration or other dependencies later
   }
   ```

### Best Practices Summary

1. **Use static methods for**:

   - Factory methods that create new instances
   - Validation and utility functions
   - Pure functions without side effects
   - Extension methods and mappers
   - Constants and configuration helpers

2. **Avoid static methods for**:

   - Methods that use dependency injection
   - Repository and service methods
   - Methods that might need instance data later
   - Controller actions (except in rare cases)

3. **Consider testability**:
   - Static methods are harder to mock
   - Use dependency injection for better testability
   - Keep static methods pure and simple

## ASP.NET Core Best Practices

### Avoid Blocking Calls

ASP.NET Core apps should process many requests simultaneously. Avoid blocking calls that could be asynchronous:

```csharp
// ✅ Good: Asynchronous operations
public class TransactionController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

// ❌ Bad: Blocking async calls
public class BadTransactionController : ControllerBase
{
    [HttpPost]
    public ActionResult<TransactionDto> CreateTransaction(CreateTransactionCommand command)
    {
        var result = _mediator.Send(command).Result; // Don't do this!
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
```

### Return Large Collections with Pagination

Don't return large collections all at once. Implement pagination:

```csharp
// ✅ Good: Paginated results
[HttpGet]
public async Task<ActionResult<PagedResult<TransactionDto>>> GetTransactions(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    CancellationToken cancellationToken = default)
{
    pageSize = Math.Min(pageSize, 100); // Limit maximum page size

    var query = new GetTransactionsQuery(page, pageSize);
    var result = await _mediator.Send(query, cancellationToken);

    return Ok(result);
}

// ✅ Good: Use IAsyncEnumerable for streaming
public async IAsyncEnumerable<TransactionDto> GetTransactionsStream(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var transaction in _repository.GetTransactionsStreamAsync(cancellationToken))
    {
        yield return _mapper.Map<TransactionDto>(transaction);
    }
}
```

### Optimize Data Access and I/O

Make all data access operations asynchronous and efficient:

```csharp
// ✅ Good: Optimized data access
public class AccountService
{
    private readonly IAccountRepository _repository;
    private readonly IMemoryCache _cache;

    public async Task<Account> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        // Try cache first
        var cacheKey = $"account_{accountId}";
        if (_cache.TryGetValue(cacheKey, out Account cachedAccount))
        {
            return cachedAccount;
        }

        // Use no-tracking query for read-only data
        var account = await _repository.GetByIdNoTrackingAsync(accountId, cancellationToken);

        // Cache for 5 minutes
        _cache.Set(cacheKey, account, TimeSpan.FromMinutes(5));

        return account;
    }
}

// ✅ Good: Minimize network round trips
public async Task<AccountWithTransactionsDto> GetAccountWithRecentTransactionsAsync(
    Guid accountId,
    CancellationToken cancellationToken)
{
    // Single query instead of multiple calls
    var accountWithTransactions = await _context.Accounts
        .Include(a => a.Transactions.OrderByDescending(t => t.CreatedAt).Take(10))
        .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

    return _mapper.Map<AccountWithTransactionsDto>(accountWithTransactions);
}
```

### Use HttpClientFactory for HTTP Connections

Pool HTTP connections properly:

```csharp
// ✅ Good: Use HttpClientFactory
public class ExternalPaymentService
{
    private readonly HttpClient _httpClient;

    public ExternalPaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/payments", request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PaymentResult>();
    }
}

// Register in Program.cs
builder.Services.AddHttpClient<ExternalPaymentService>(client =>
{
    client.BaseAddress = new Uri("https://api.paymentprovider.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### Avoid Large Object Allocations

Minimize allocations in hot code paths:

```csharp
// ✅ Good: Use ArrayPool for large arrays
public class CsvReportGenerator
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    public async Task<byte[]> GenerateReportAsync(IEnumerable<Transaction> transactions)
    {
        const int bufferSize = 1024 * 1024; // 1MB
        var buffer = _arrayPool.Rent(bufferSize);

        try
        {
            using var stream = new MemoryStream(buffer);
            await WriteCsvDataAsync(stream, transactions);
            return stream.ToArray();
        }
        finally
        {
            _arrayPool.Return(buffer);
        }
    }
}

// ✅ Good: Use StringBuilder for string concatenation
public string FormatTransactionSummary(IEnumerable<Transaction> transactions)
{
    var sb = new StringBuilder();
    foreach (var transaction in transactions)
    {
        sb.AppendLine($"{transaction.Date:yyyy-MM-dd}: {transaction.Amount:C} - {transaction.Description}");
    }
    return sb.ToString();
}
```

### Handle HttpContext Properly

Never store HttpContext in fields or access it from multiple threads:

```csharp
// ✅ Good: Pass data explicitly
[HttpPost]
public async Task<ActionResult> ProcessTransactionAsync(
    CreateTransactionCommand command)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

    // Pass explicit parameters instead of HttpContext
    var result = await _transactionService.ProcessTransactionAsync(
        command, userId, ipAddress);

    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
}

// ❌ Bad: Storing HttpContext
public class BadService
{
    private readonly HttpContext _context; // Don't do this!

    public BadService(IHttpContextAccessor accessor)
    {
        _context = accessor.HttpContext; // Don't store HttpContext
    }
}
```

### Use Strongly-Typed Response Headers (ASP0015)

Always use strongly-typed properties for HTTP response headers instead of string-based access:

```csharp
// ✅ Good: Use strongly-typed header properties
public class SecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Use strongly-typed properties
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers.XXSSProtection = "1; mode=block";
        headers.ReferrerPolicy = "strict-origin-when-cross-origin";
        headers.ContentSecurityPolicy = "default-src 'self'";

        // For headers without strongly-typed properties, use string access
        headers["Permissions-Policy"] = "camera=(), microphone=()";

        await _next(context);
    }
}

// ❌ Bad: String-based header access when strongly-typed properties exist
public class BadSecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Don't use string access when strongly-typed properties exist
        headers["X-Content-Type-Options"] = "nosniff"; // Use headers.XContentTypeOptions instead
        headers["X-Frame-Options"] = "DENY";          // Use headers.XFrameOptions instead
        headers["X-XSS-Protection"] = "1; mode=block"; // Use headers.XXSSProtection instead

        await _next(context);
    }
}
```

### Common Strongly-Typed Header Properties

```csharp
// Available strongly-typed header properties in ASP.NET Core
public void SetResponseHeaders(HttpContext context)
{
    var headers = context.Response.Headers;

    // Security headers
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers.XXSSProtection = "1; mode=block";
    headers.ReferrerPolicy = "strict-origin-when-cross-origin";
    headers.ContentSecurityPolicy = "default-src 'self'";

    // Caching headers
    headers.CacheControl = "no-cache, no-store, must-revalidate";
    headers.Pragma = "no-cache";
    headers.Expires = "0";
    headers.ETag = "\"version-123\"";
    headers.LastModified = DateTimeOffset.UtcNow.ToString("R");

    // Content headers
    headers.ContentType = "application/json";
    headers.ContentLength = 1024;
    headers.ContentEncoding = "gzip";
    headers.ContentDisposition = "attachment; filename=data.json";

    // CORS headers
    headers.AccessControlAllowOrigin = "*";
    headers.AccessControlAllowMethods = "GET, POST, PUT, DELETE";
    headers.AccessControlAllowHeaders = "Content-Type, Authorization";

    // Custom headers (when no strongly-typed property exists)
    headers["Custom-Header"] = "custom-value";
    headers["Permissions-Policy"] = "camera=(), microphone=()";
}
```

### Implement Proper Error Handling

Don't expose internal details in error responses:

```csharp
// ✅ Good: Secure error handling
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException => new {
                Type = "validation_error",
                Title = "Validation Failed",
                Status = 400,
                Detail = "One or more validation errors occurred"
            },
            DomainException => new {
                Type = "business_error",
                Title = "Business Rule Violation",
                Status = 400,
                Detail = exception.Message
            },
            _ => new {
                Type = "internal_error",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "An unexpected error occurred"
            }
        };

        context.Response.StatusCode = response.Status;
        context.Response.Headers.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### Use Background Services for Long-Running Tasks

Don't block HTTP requests with long-running operations:

```csharp
// ✅ Good: Background service for long-running tasks
public class TransactionProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionProcessingService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var transactionQueue = scope.ServiceProvider.GetRequiredService<ITransactionQueue>();

            var pendingTransactions = await transactionQueue.DequeueBatchAsync(10, stoppingToken);

            var tasks = pendingTransactions.Select(ProcessTransactionAsync);
            await Task.WhenAll(tasks);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

// ✅ Good: Controller queues work and returns immediately
[HttpPost("bulk")]
public async Task<ActionResult> QueueBulkTransactions(
    [FromBody] IEnumerable<CreateTransactionCommand> commands)
{
    var jobId = Guid.NewGuid();
    await _transactionQueue.EnqueueBulkAsync(jobId, commands);

    return Accepted(new { JobId = jobId, Status = "Queued" });
}
```

## Guidelines Summary

1. **Always use meaningful names** that express intent
2. **Keep methods small** and focused on a single responsibility
3. **Use guard clauses** for early validation and error returns
4. **Prefer composition over inheritance** where possible
5. **Make classes immutable** when possible (especially value objects)
6. **Use async/await** for all I/O operations
7. **Handle errors gracefully** with Result pattern or appropriate exceptions
8. **Write self-documenting code** with clear variable and method names
9. **Use dependency injection** consistently throughout the application
10. **Follow SOLID principles** in all design decisions
11. **Avoid blocking calls** in ASP.NET Core applications
12. **Implement pagination** for large data sets
13. **Use HttpClientFactory** for external HTTP calls
14. **Minimize large object allocations** in hot code paths
15. **Handle HttpContext properly** without storing or sharing across threads
