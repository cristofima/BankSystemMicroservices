# CQRS Implementation Guidelines

## Overview

Command Query Responsibility Segregation (CQRS) separates read and write operations in your application. This pattern allows for better scalability, maintainability, and separation of concerns in the Bank System Microservices project.

## Command Pattern

### Command Structure

Commands represent write operations and should be immutable records:

```csharp
// Command - immutable record
public record CreateDepositCommand(
    Guid AccountId,
    decimal Amount,
    string Currency,
    string Description,
    string Reference) : IRequest<Result<TransactionDto>>;
```

### Command Validation

Use FluentValidation for command validation:

```csharp
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
```

### Command Handler Implementation

Command handlers contain the business logic for write operations:

```csharp
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
```

## Query Pattern

### Query Structure

Queries represent read operations and should also be immutable records:

```csharp
public record GetAccountTransactionsQuery(
    Guid AccountId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<Result<PagedList<TransactionDto>>>;
```

### Query Handler Implementation

Query handlers should be optimized for read operations and can use caching:

```csharp
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

## Best Practices

### Command Best Practices

1. **Immutable Records**: Use record types for commands to ensure immutability
2. **Validation**: Always validate commands using FluentValidation
3. **Single Responsibility**: Each command should represent one business operation
4. **Return Results**: Use Result pattern for better error handling
5. **Logging**: Log important business events and errors
6. **Domain Events**: Publish domain events after successful operations

### Query Best Practices

1. **Read-Only**: Queries should never modify state
2. **Caching**: Use caching for frequently accessed data
3. **Pagination**: Always implement pagination for large result sets
4. **Projection**: Select only the data you need
5. **AsNoTracking**: Use AsNoTracking for better performance in read-only scenarios

### General CQRS Guidelines

1. **Separate Models**: Use different models for read and write operations
2. **Eventual Consistency**: Accept that read models may be slightly behind
3. **Event Sourcing**: Consider event sourcing for audit trails
4. **Separate Databases**: Consider separate read and write databases for scalability
5. **MediatR Integration**: Use MediatR for clean separation of concerns

## Registration in DI Container

```csharp
// Program.cs
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateDepositCommand).Assembly));
services.AddValidatorsFromAssembly(typeof(CreateDepositCommandValidator).Assembly);

// Add pipeline behaviors
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));
```

---

_This guideline follows the principles outlined in [Clean Code Guidelines](./clean-code.md) and [SOLID Principles](./solid-principles.md)._
