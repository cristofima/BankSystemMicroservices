# .NET 9 Microservices Development Guidelines

## Table of Contents

- [Project Structure](#project-structure)
- [Clean Architecture Implementation](#clean-architecture-implementation)
- [SOLID Principles](#solid-principles)
- [Domain-Driven Design (DDD)](#domain-driven-design-ddd)
- [Event-Driven Architecture (EDA)](#event-driven-architecture-eda)
- [CQRS Pattern Implementation](#cqrs-pattern-implementation)
- [Code Quality Standards](#code-quality-standards)
- [Azure Integration Patterns](#azure-integration-patterns)
- [Testing Strategies](#testing-strategies)

## Project Structure

### Solution-Level Structure

```
/BankSystemMicroservices/
├── src/
│   ├── BankSystem.sln
│   ├── services/
│   │   ├── Security/           # Authentication & Authorization
│   │   ├── Account/           # Account Management
│   │   ├── Transaction/       # Transaction Processing (Commands)
│   │   └── Movement/          # Movement History (Queries)
│   ├── shared/
│   │   ├── BankSystem.Shared.Events/     # Common domain events
│   │   ├── BankSystem.Shared.Contracts/  # Common interfaces & DTOs
│   │   └── BankSystem.Shared.Infrastructure/ # Common infrastructure
│   └── client/
│       └── BankSystem.WebApp/    # Angular Client Application
├── tests/
├── docs/
├── iac/                      # Infrastructure as Code (Terraform/Bicep)
└── build/                    # CI/CD Pipeline configurations
```

### Microservice Internal Structure

Each microservice follows Clean Architecture with this structure:

```
/ServiceName/
├── src/
│   ├── ServiceName.Api/              # Presentation Layer
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Extensions/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Dockerfile
│   │
│   ├── ServiceName.Application/      # Application Layer
│   │   ├── Commands/                 # CQRS Commands
│   │   ├── Queries/                  # CQRS Queries
│   │   ├── Handlers/                 # Command & Query Handlers
│   │   ├── DTOs/                     # Data Transfer Objects
│   │   ├── Interfaces/               # Application Interfaces
│   │   ├── Validators/               # FluentValidation Validators
│   │   ├── Mappers/                  # AutoMapper Profiles
│   │   └── DependencyInjection.cs   # Service Registration
│   │
│   ├── ServiceName.Domain/           # Domain Layer
│   │   ├── Entities/                 # Domain Entities
│   │   ├── ValueObjects/             # Value Objects
│   │   ├── Events/                   # Domain Events
│   │   ├── Enums/                    # Domain Enumerations
│   │   ├── Exceptions/               # Domain Exceptions
│   │   └── Interfaces/               # Domain Interfaces
│   │
│   └── ServiceName.Infrastructure/   # Infrastructure Layer
│       ├── Data/                     # EF Core DbContext & Configurations
│       ├── Repositories/             # Repository Implementations
│       ├── Messaging/                # Azure Service Bus Implementation
│       ├── Services/                 # External Service Integrations
│       └── DependencyInjection.cs   # Infrastructure Service Registration
│
└── tests/
    ├── ServiceName.Application.UnitTests/
    ├── ServiceName.Domain.UnitTests/
    ├── ServiceName.Infrastructure.IntegrationTests/
    └── ServiceName.Api.IntegrationTests/
```

## Clean Architecture Implementation

### Layer Dependencies

- **API Layer** → Application Layer
- **Application Layer** → Domain Layer
- **Infrastructure Layer** → Application Layer + Domain Layer
- **Domain Layer** → No dependencies (pure business logic)

### Dependency Injection Setup

#### Program.cs Structure

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add layers
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

var app = builder.Build();

// Configure pipeline
app.UseExceptionHandling();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program
{ }
```

#### Layer Registration Patterns

```csharp
// Application Layer
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}

// Infrastructure Layer
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ServiceDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();

        return services;
    }
}
```

## SOLID Principles

### Single Responsibility Principle (SRP)

```csharp
// ❌ Bad: Multiple responsibilities
public class TransactionService
{
    public void ProcessTransaction(Transaction transaction) { }
    public void SendNotification(string email, string message) { }
    public void LogTransaction(Transaction transaction) { }
}

// ✅ Good: Single responsibility
public class TransactionProcessor
{
    public void ProcessTransaction(Transaction transaction) { }
}

public class NotificationService
{
    public void SendNotification(string email, string message) { }
}

public class TransactionLogger
{
    public void LogTransaction(Transaction transaction) { }
}
```

### Open/Closed Principle (OCP)

```csharp
// Base abstraction
public abstract class TransactionValidator
{
    public abstract ValidationResult Validate(Transaction transaction);
}

// Extensible implementations
public class DepositValidator : TransactionValidator
{
    public override ValidationResult Validate(Transaction transaction)
    {
        // Deposit-specific validation
    }
}

public class WithdrawalValidator : TransactionValidator
{
    public override ValidationResult Validate(Transaction transaction)
    {
        // Withdrawal-specific validation
    }
}
```

### Interface Segregation Principle (ISP)

```csharp
// ❌ Bad: Fat interface
public interface IUserService
{
    Task<User> GetUserAsync(Guid id);
    Task<User> CreateUserAsync(User user);
    Task SendPasswordResetEmailAsync(string email);
    Task<bool> ValidatePasswordAsync(string password);
}

// ✅ Good: Segregated interfaces
public interface IUserReader
{
    Task<User> GetUserAsync(Guid id);
}

public interface IUserWriter
{
    Task<User> CreateUserAsync(User user);
}

public interface IPasswordService
{
    Task SendPasswordResetEmailAsync(string email);
    Task<bool> ValidatePasswordAsync(string password);
}
```

### Dependency Inversion Principle (DIP)

```csharp
// High-level module depends on abstraction
public class TransactionHandler
{
    private readonly ITransactionRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public TransactionHandler(ITransactionRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }
}

// Low-level modules implement abstractions
public class SqlTransactionRepository : ITransactionRepository
{
    // Implementation details
}

public class ServiceBusEventPublisher : IEventPublisher
{
    // Implementation details
}
```

## Domain-Driven Design (DDD)

### Entity Pattern

```csharp
public class Account : EntityBase<Guid>
{
    private readonly List<Transaction> _transactions = new();

    public string AccountNumber { get; private set; }
    public decimal Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    public Guid OwnerId { get; private set; }

    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private Account() { } // EF Core constructor

    public Account(string accountNumber, Guid ownerId)
    {
        Id = Guid.NewGuid();
        AccountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        OwnerId = ownerId;
        Balance = 0;
        Status = AccountStatus.Active;

        AddDomainEvent(new AccountCreatedEvent(Id, accountNumber, ownerId));
    }

    public void Deposit(decimal amount, string description)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Deposit amount must be positive");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Cannot deposit to inactive account");

        Balance += amount;
        var transaction = new Transaction(Id, amount, TransactionType.Deposit, description);
        _transactions.Add(transaction);

        AddDomainEvent(new TransactionCreatedEvent(transaction.Id, Id, amount, TransactionType.Deposit));
    }

    public void Withdraw(decimal amount, string description)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Withdrawal amount must be positive");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Cannot withdraw from inactive account");

        if (Balance < amount)
            throw new InsufficientFundsException($"Insufficient funds. Current balance: {Balance}");

        Balance -= amount;
        var transaction = new Transaction(Id, -amount, TransactionType.Withdrawal, description);
        _transactions.Add(transaction);

        AddDomainEvent(new TransactionCreatedEvent(transaction.Id, Id, amount, TransactionType.Withdrawal));
    }
}
```

### Value Object Pattern

```csharp
public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract different currencies");

        return new Money(Amount - other.Amount, Currency);
    }
}

public record AccountNumber(string Value)
{
    public AccountNumber(string value) : this(value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Account number cannot be empty", nameof(value));

        if (value.Length != 10)
            throw new ArgumentException("Account number must be 10 digits", nameof(value));

        if (!value.All(char.IsDigit))
            throw new ArgumentException("Account number must contain only digits", nameof(value));

        Value = value;
    }
}
```

### Domain Events

```csharp
public abstract record DomainEvent(Guid Id, DateTime OccurredAt) : IDomainEvent
{
    protected DomainEvent() : this(Guid.NewGuid(), DateTime.UtcNow) { }
}

public record AccountCreatedEvent(
    Guid AccountId,
    string AccountNumber,
    Guid OwnerId) : DomainEvent;

public record TransactionCreatedEvent(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    TransactionType Type) : DomainEvent;

public record AccountBalanceUpdatedEvent(
    Guid AccountId,
    decimal NewBalance,
    decimal PreviousBalance) : DomainEvent;
```

### Aggregate Root Base Class

```csharp
public abstract class AggregateRoot<TId> : EntityBase<TId>
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

public abstract class EntityBase<TId>
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected EntityBase()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityBase<TId> entity && EqualityComparer<TId>.Default.Equals(Id, entity.Id);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }
}
```

## Event-Driven Architecture (EDA)

### Event Publisher Implementation

```csharp
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDomainEvent;
}

public class ServiceBusEventPublisher : IEventPublisher
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<ServiceBusEventPublisher> _logger;

    public ServiceBusEventPublisher(ServiceBusClient serviceBusClient, ILogger<ServiceBusEventPublisher> logger)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        var topicName = GetTopicName<T>();
        var sender = _serviceBusClient.CreateSender(topicName);

        var message = new ServiceBusMessage(JsonSerializer.Serialize(@event))
        {
            MessageId = @event.Id.ToString(),
            CorrelationId = Activity.Current?.Id,
            Subject = typeof(T).Name
        };

        try
        {
            await sender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Published event {EventType} with ID {EventId}", typeof(T).Name, @event.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} with ID {EventId}", typeof(T).Name, @event.Id);
            throw;
        }
    }

    private static string GetTopicName<T>() => typeof(T).Name.ToLowerInvariant();
}
```

### Event Handler Implementation

```csharp
public interface IEventHandler<in T> where T : IDomainEvent
{
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}

public class TransactionCreatedEventHandler : IEventHandler<TransactionCreatedEvent>
{
    private readonly IMovementRepository _movementRepository;
    private readonly ILogger<TransactionCreatedEventHandler> _logger;

    public TransactionCreatedEventHandler(
        IMovementRepository movementRepository,
        ILogger<TransactionCreatedEventHandler> logger)
    {
        _movementRepository = movementRepository;
        _logger = logger;
    }

    public async Task HandleAsync(TransactionCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var movement = new Movement(
                @event.TransactionId,
                @event.AccountId,
                @event.Amount,
                @event.Type,
                @event.OccurredAt);

            await _movementRepository.AddAsync(movement, cancellationToken);

            _logger.LogInformation("Created movement record for transaction {TransactionId}", @event.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle TransactionCreatedEvent for transaction {TransactionId}", @event.TransactionId);
            throw;
        }
    }
}
```

## CQRS Pattern Implementation

### Command Pattern

```csharp
public record CreateDepositCommand(
    Guid AccountId,
    decimal Amount,
    string Description) : IRequest<TransactionDto>;

public class CreateDepositCommandValidator : AbstractValidator<CreateDepositCommand>
{
    public CreateDepositCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(50000)
            .WithMessage("Amount cannot exceed $50,000");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");
    }
}

public class CreateDepositCommandHandler : IRequestHandler<CreateDepositCommand, TransactionDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateDepositCommandHandler> _logger;

    public CreateDepositCommandHandler(
        IAccountRepository accountRepository,
        IEventPublisher eventPublisher,
        IMapper mapper,
        ILogger<CreateDepositCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TransactionDto> Handle(CreateDepositCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
            throw new NotFoundException($"Account with ID {request.AccountId} not found");

        account.Deposit(request.Amount, request.Description);

        await _accountRepository.UpdateAsync(account, cancellationToken);

        // Publish domain events
        foreach (var domainEvent in account.DomainEvents)
        {
            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
        }

        account.ClearDomainEvents();

        var transaction = account.Transactions.Last();
        var result = _mapper.Map<TransactionDto>(transaction);

        _logger.LogInformation("Deposit of {Amount} processed for account {AccountId}", request.Amount, request.AccountId);

        return result;
    }
}
```

### Query Pattern

```csharp
public record GetAccountMovementsQuery(
    Guid AccountId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<MovementDto>>;

public class GetAccountMovementsQueryValidator : AbstractValidator<GetAccountMovementsQuery>
{
    public GetAccountMovementsQueryValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than zero");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("From date must be less than or equal to To date");
    }
}

public class GetAccountMovementsQueryHandler : IRequestHandler<GetAccountMovementsQuery, PagedResult<MovementDto>>
{
    private readonly IMovementRepository _movementRepository;
    private readonly IMapper _mapper;

    public GetAccountMovementsQueryHandler(IMovementRepository movementRepository, IMapper mapper)
    {
        _movementRepository = movementRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<MovementDto>> Handle(GetAccountMovementsQuery request, CancellationToken cancellationToken)
    {
        var movements = await _movementRepository.GetPagedByAccountIdAsync(
            request.AccountId,
            request.FromDate,
            request.ToDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return _mapper.Map<PagedResult<MovementDto>>(movements);
    }
}
```

## Code Quality Standards

### Error Handling

```csharp
// Custom Exceptions
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(string message) : base(message) { }
}

// Global Exception Middleware
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            NotFoundException => new { StatusCode = 404, Message = exception.Message },
            ValidationException => new { StatusCode = 400, Message = exception.Message },
            DomainException => new { StatusCode = 400, Message = exception.Message },
            _ => new { StatusCode = 500, Message = "An internal server error occurred" }
        };

        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### Validation Pattern

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Any())
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```

### Logging Patterns

```csharp
// Structured Logging with Serilog
public static class Log
{
    public static class Events
    {
        public static readonly EventId TransactionProcessed = new(1001, "TransactionProcessed");
        public static readonly EventId AccountCreated = new(1002, "AccountCreated");
        public static readonly EventId UserAuthenticated = new(1003, "UserAuthenticated");
    }
}

// Usage in handlers
_logger.LogInformation(Log.Events.TransactionProcessed,
    "Transaction {TransactionId} processed for account {AccountId} with amount {Amount}",
    transaction.Id, account.Id, amount);
```

## Azure Integration Patterns

### Configuration

```csharp
// appsettings.json structure
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;",
    "ServiceBus": "Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=..."
  },
  "Azure": {
    "ServiceBus": {
      "ConnectionString": "...",
      "Topics": {
        "TransactionEvents": "transaction-events",
        "AccountEvents": "account-events"
      }
    },
    "KeyVault": {
      "VaultUrl": "https://your-keyvault.vault.azure.net/"
    },
    "ApplicationInsights": {
      "ConnectionString": "..."
    }
  },
  "Jwt": {
    "Issuer": "https://your-api.com",
    "Audience": "your-api",
    "Key": "your-secret-key"
  }
}
```

### Service Bus Integration

```csharp
public static class ServiceBusExtensions
{
    public static IServiceCollection AddServiceBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ServiceBusClient>(provider =>
        {
            var connectionString = configuration.GetConnectionString("ServiceBus");
            return new ServiceBusClient(connectionString);
        });

        services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();

        return services;
    }
}
```

### Health Checks

```csharp
public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(configuration.GetConnectionString("DefaultConnection"), name: "database")
            .AddAzureServiceBusTopic(
                configuration.GetConnectionString("ServiceBus"),
                "transaction-events",
                name: "servicebus")
            .AddApplicationInsightsPublisher();

        return services;
    }
}
```

## Testing Strategies

### Unit Testing Example

```csharp
[TestFixture]
public class AccountTests
{
    [Test]
    public void Deposit_ValidAmount_ShouldIncreaseBalance()
    {
        // Arrange
        var account = new Account("1234567890", Guid.NewGuid());
        var initialBalance = account.Balance;
        var depositAmount = 100m;

        // Act
        account.Deposit(depositAmount, "Test deposit");

        // Assert
        Assert.That(account.Balance, Is.EqualTo(initialBalance + depositAmount));
        Assert.That(account.DomainEvents, Has.Count.EqualTo(2)); // AccountCreated + TransactionCreated
        Assert.That(account.DomainEvents.Last(), Is.TypeOf<TransactionCreatedEvent>());
    }

    [Test]
    public void Withdraw_InsufficientFunds_ShouldThrowException()
    {
        // Arrange
        var account = new Account("1234567890", Guid.NewGuid());
        var withdrawAmount = 100m;

        // Act & Assert
        Assert.Throws<InsufficientFundsException>(() => account.Withdraw(withdrawAmount, "Test withdrawal"));
    }
}
```

### Integration Testing Example

```csharp
[TestFixture]
public class TransactionControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransactionControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task CreateDeposit_ValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var command = new CreateDepositCommand(Guid.NewGuid(), 100m, "Test deposit");
        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/transactions/deposit", content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TransactionDto>(responseContent);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Amount, Is.EqualTo(100m));
    }
}
```

Remember to always follow these patterns and principles when developing the Bank System Microservices. Each microservice should be a cohesive, independently deployable unit that follows these architectural guidelines.
