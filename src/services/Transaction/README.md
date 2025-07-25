# Transaction Service

## Overview

The Transaction Service is a critical microservice in the Bank System that manages all financial transaction operations. It serves as the central processing unit for recording, validating, and tracking all monetary movements within the banking system while ensuring ACID compliance and regulatory requirements.

**Core Purpose**: Process and record all financial transactions with complete audit trails, ensuring data integrity and regulatory compliance.

## Architecture

This service follows Clean Architecture principles with clear separation of concerns:

- **API Layer**: REST controllers, middleware, and request/response handling
- **Application Layer**: Transaction processing logic, command/query handlers, validation
- **Domain Layer**: Transaction entities, business rules, domain events
- **Infrastructure Layer**: Database operations, external service integrations, event publishing

## Key Responsibilities

### What This Service DOES:

- ✅ **Process all transaction types** (deposits, withdrawals, transfers, payments)
- ✅ **Validate transaction requests** against business rules and limits
- ✅ **Record transaction history** with complete audit trails
- ✅ **Calculate transaction fees** based on transaction type and amount
- ✅ **Enforce transaction limits** (daily, monthly, per-transaction)
- ✅ **Generate transaction confirmations** and receipts
- ✅ **Publish transaction events** for other services to consume
- ✅ **Handle transaction reversals** and corrections
- ✅ **Maintain transaction status** throughout the processing lifecycle
- ✅ **Ensure ACID compliance** for all financial operations
- ✅ **Provide transaction reporting** and analytics
- ✅ **Implement fraud detection** basic checks and patterns

### What This Service DOES NOT Do:

- ❌ **Account balance management** → Handled by Account Service
- ❌ **User authentication/authorization** → Handled by Security Service
- ❌ **Account creation or management** → Handled by Account Service
- ❌ **Customer management** → Handled by Account Service
- ❌ **Complex notifications** → Handled by Notification Service
- ❌ **Detailed reporting and analytics** → Handled by Reporting Service
- ❌ **External payment processing** → Handled by Movement Service
- ❌ **Currency conversion** → Handled by Movement Service

## Service Communication

### Synchronous Communication (REST APIs)

- **Account Service**: Validates account existence and retrieves account details
- **Security Service**: Validates user permissions and authentication tokens
- **Movement Service**: Coordinates external payment processing

### Asynchronous Communication (Events)

**Published Events:**

- `TransactionCreated`: When a new transaction is successfully recorded
- `TransactionCompleted`: When a transaction processing is finished
- `TransactionFailed`: When a transaction fails validation or processing
- `TransactionReversed`: When a transaction is reversed or corrected
- `SuspiciousTransactionDetected`: When fraud detection flags a transaction

**Consumed Events:**

- `AccountCreated`: Updates transaction limits for new accounts
- `AccountStatusChanged`: Adjusts transaction processing based on account status
- `ExternalPaymentCompleted`: Updates transaction status from Movement Service

### Integration Points

```csharp
// Account Service Integration
public interface IAccountServiceClient
{
    Task<AccountDto> GetAccountAsync(Guid accountId);
    Task<bool> ValidateAccountStatusAsync(Guid accountId);
    Task<decimal> GetAvailableBalanceAsync(Guid accountId);
}

// Movement Service Integration
public interface IMovementServiceClient
{
    Task<MovementResult> ProcessExternalPaymentAsync(ExternalPaymentRequest request);
    Task<MovementStatus> GetMovementStatusAsync(Guid movementId);
}
```

# Transaction Service

The Transaction Service handles all financial transaction processing within the Bank System Microservices architecture. It implements the Command side of the CQRS pattern, processing deposits, withdrawals, and transfers while publishing events for other services to maintain data consistency.

## 🎯 Service Overview

### Responsibilities

- **Transaction Processing**: Execute deposits, withdrawals, and transfers
- **Transaction Validation**: Validate business rules and constraints
- **Event Publishing**: Publish transaction events for downstream services
- **Transaction Recording**: Maintain transaction records and audit trails
- **Fraud Detection**: Basic fraud prevention and suspicious activity detection

### Domain Boundaries

- Financial transaction processing
- Transaction validation and business rules
- Transaction state management
- Transaction-related events

## 🏗️ Architecture

### Clean Architecture Layers

```
Transaction.Api/           # Presentation Layer
├── Controllers/           # API Controllers
├── Middleware/           # Transaction middleware
├── Extensions/           # Service extensions
└── Program.cs           # Application startup

Transaction.Application/   # Application Layer
├── Commands/            # CQRS Commands (CreateDeposit, CreateWithdrawal, CreateTransfer)
├── Handlers/           # Command Handlers
├── DTOs/              # Data Transfer Objects
├── Interfaces/        # Application Interfaces
├── Validators/        # FluentValidation Validators
└── Mappers/          # AutoMapper Profiles

Transaction.Domain/        # Domain Layer
├── Entities/            # Domain Entities (Transaction, TransactionBatch)
├── ValueObjects/       # Value Objects (Money, TransactionReference)
├── Events/            # Domain Events (TransactionCreated, TransactionCompleted)
├── Enums/            # Domain Enumerations (TransactionType, TransactionStatus)
└── Exceptions/       # Domain Exceptions

Transaction.Infrastructure/ # Infrastructure Layer
├── Data/              # EF Core DbContext
├── Repositories/      # Repository Implementations
├── Messaging/         # Event Publishing (Azure Service Bus)
├── Services/          # External Service Integrations
└── Fraud/            # Fraud detection services
```

## 🔧 Features

### Transaction Types

- **Deposits**: Add funds to an account
- **Withdrawals**: Remove funds from an account
- **Transfers**: Move funds between accounts
- **Reversals**: Reverse previous transactions

### Validation & Security

- **Business Rule Validation**: Account status, balance limits, daily limits
- **Fraud Detection**: Suspicious pattern detection
- **Duplicate Prevention**: Idempotency key validation
- **Authorization**: Transaction amount limits based on user roles

### Event-Driven Communication

- **Transaction Events**: Publish events for balance updates
- **Asynchronous Processing**: Non-blocking transaction processing
- **Event Sourcing**: Maintain complete transaction history
- **Saga Pattern**: Handle complex multi-step transactions

## 🔌 API Endpoints

### Transaction Processing Endpoints

#### POST /api/transactions/deposit

Process a deposit transaction.

**Request Body:**

```json
{
  "accountId": "guid",
  "amount": 500.0,
  "currency": "USD",
  "description": "Salary deposit",
  "reference": "SAL-20240115-001",
  "idempotencyKey": "unique-key-123"
}
```

**Response:**

```json
{
  "transactionId": "guid",
  "accountId": "guid",
  "amount": 500.0,
  "currency": "USD",
  "type": "Deposit",
  "status": "Completed",
  "description": "Salary deposit",
  "reference": "SAL-20240115-001",
  "timestamp": "2024-01-15T10:30:00Z",
  "balanceAfter": 2000.0
}
```

#### POST /api/transactions/withdrawal

Process a withdrawal transaction.

**Request Body:**

```json
{
  "accountId": "guid",
  "amount": 200.0,
  "currency": "USD",
  "description": "ATM withdrawal",
  "reference": "ATM-20240115-001",
  "idempotencyKey": "unique-key-124"
}
```

#### POST /api/transactions/transfer

Process a transfer between accounts.

**Request Body:**

```json
{
  "fromAccountId": "guid",
  "toAccountId": "guid",
  "amount": 300.0,
  "currency": "USD",
  "description": "Transfer to savings",
  "reference": "TRF-20240115-001",
  "idempotencyKey": "unique-key-125"
}
```

#### GET /api/transactions/{transactionId}

Get transaction details by ID.

#### POST /api/transactions/{transactionId}/reverse

Reverse a completed transaction.

## 🗄️ Data Model

### Transaction Entity

```csharp
public class Transaction : AggregateRoot<Guid>
{
    public Guid AccountId { get; private set; }
    public Guid? ToAccountId { get; private set; } // For transfers
    public Money Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string Description { get; private set; }
    public string Reference { get; private set; }
    public string IdempotencyKey { get; private set; }
    public DateTime ProcessedAt { get; private set; }
    public Guid? ReversalTransactionId { get; private set; }

    // Domain methods
    public void Complete();
    public void Fail(string reason);
    public Transaction CreateReversal(string reason);
}
```

### Value Objects

```csharp
public record Money(decimal Amount, Currency Currency)
{
    public static Money Zero(Currency currency) => new(0, currency);
    public Money Add(Money other) => /* implementation */;
    public Money Subtract(Money other) => /* implementation */;
}

public record TransactionReference(string Value)
{
    // Validation and formatting logic
}
```

### Domain Events

```csharp
public record TransactionCreatedEvent(
    Guid TransactionId,
    Guid AccountId,
    Guid? ToAccountId,
    Money Amount,
    TransactionType Type,
    string Reference,
    DateTime Timestamp) : DomainEvent;

public record TransactionCompletedEvent(
    Guid TransactionId,
    Guid AccountId,
    Money Amount,
    TransactionType Type,
    DateTime CompletedAt) : DomainEvent;

public record TransactionFailedEvent(
    Guid TransactionId,
    Guid AccountId,
    string FailureReason,
    DateTime FailedAt) : DomainEvent;
```

## ⚙️ Configuration

### Database Schema

- **Transactions**: Main transaction records
- **TransactionEvents**: Event sourcing table
- **IdempotencyKeys**: Duplicate prevention
- **FraudAlerts**: Suspicious activity tracking

### Business Rules Configuration

```json
{
  "TransactionLimits": {
    "DailyWithdrawalLimit": 5000.0,
    "SingleTransactionLimit": 10000.0,
    "MonthlyTransferLimit": 50000.0
  },
  "FraudDetection": {
    "SuspiciousAmountThreshold": 10000.0,
    "MaxTransactionsPerHour": 10,
    "VelocityCheckEnabled": true
  }
}
```

## 🔒 Security & Validation

### Transaction Validation

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
            .WithMessage("Amount exceeds maximum limit");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required");
    }
}
```

### Fraud Detection

```csharp
public class FraudDetectionService : IFraudDetectionService
{
    public async Task<FraudCheckResult> CheckTransactionAsync(
        CreateTransactionCommand command)
    {
        var checks = new List<IFraudCheck>
        {
            new VelocityCheck(),
            new AmountThresholdCheck(),
            new PatternAnalysisCheck(),
            new GeolocationCheck()
        };

        foreach (var check in checks)
        {
            var result = await check.EvaluateAsync(command);
            if (result.IsHighRisk)
            {
                return FraudCheckResult.Blocked(result.Reason);
            }
        }

        return FraudCheckResult.Approved();
    }
}
```

## 🔄 Transaction Processing Flow

### Deposit Flow

1. **Receive Request**: API receives deposit command
2. **Validate Request**: Business rules and fraud checks
3. **Process Transaction**: Create transaction record
4. **Publish Events**: TransactionCreatedEvent to Service Bus
5. **Update Balance**: Account Service updates balance
6. **Complete Transaction**: Mark transaction as completed
7. **Publish Completion**: TransactionCompletedEvent

### Withdrawal Flow

1. **Receive Request**: API receives withdrawal command
2. **Validate Request**: Business rules, balance, and fraud checks
3. **Reserve Funds**: Temporarily hold the amount
4. **Process Transaction**: Create transaction record
5. **Publish Events**: TransactionCreatedEvent
6. **Update Balance**: Account Service updates balance
7. **Complete Transaction**: Release hold and complete

### Transfer Flow

1. **Receive Request**: API receives transfer command
2. **Validate Accounts**: Check both source and destination accounts
3. **Create Transfer Saga**: Manage multi-step process
4. **Debit Source**: Withdraw from source account
5. **Credit Destination**: Deposit to destination account
6. **Complete Transfer**: Mark both transactions as completed

## 🧪 Testing Strategy

### Unit Tests

```csharp
using Xunit;
using Moq;
using Transaction.Application.Features.Transactions.Commands;
using Transaction.Application.Features.Transactions.Queries;
using Transaction.Domain.Entities;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockAccountService = new Mock<IAccountService>();
        _service = new TransactionService(_mockRepository.Object, _mockAccountService.Object);
    }

    [Fact]
    public async Task CreateTransaction_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            Type = TransactionType.Deposit,
            Description = "Test deposit"
        };

        _mockAccountService.Setup(x => x.GetAccountAsync(command.AccountId))
            .ReturnsAsync(new AccountDto { Id = command.AccountId, Balance = 1000m });

        // Act
        var result = await _service.CreateTransactionAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<TransactionEntity>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateTransaction_WithInvalidAmount_ShouldReturnFailure(decimal amount)
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = amount,
            Type = TransactionType.Deposit,
            Description = "Test"
        };

        // Act
        var result = await _service.CreateTransactionAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Amount must be positive", result.Error);
    }

    [Fact]
    public async Task GetTransactionHistory_WithValidAccountId_ShouldReturnTransactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var expectedTransactions = new List<TransactionEntity>
        {
            new() { Id = Guid.NewGuid(), AccountId = accountId, Amount = 100m },
            new() { Id = Guid.NewGuid(), AccountId = accountId, Amount = 200m }
        };

        _mockRepository.Setup(x => x.GetByAccountIdAsync(accountId))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _service.GetTransactionHistoryAsync(accountId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }
}
```

### Integration Tests

```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

public class TransactionControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransactionControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransaction_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            Type = "Deposit",
            Description = "Test transaction"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TransactionDto>(content);
        Assert.NotNull(result);
        Assert.Equal(request.Amount, result.Amount);
    }

    [Fact]
    public async Task GetTransactions_WithPagination_ShouldReturnPagedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/transactions?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<TransactionDto>>(content);
        Assert.NotNull(result);
        Assert.True(result.Data.Any());
    }

    [Fact]
    public async Task GetTransaction_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/transactions/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

### Domain Testing

```csharp
using Xunit;
using Transaction.Domain.Entities;
using Transaction.Domain.ValueObjects;

public class TransactionEntityTests
{
    [Fact]
    public void CreateTransaction_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = new Money(100m, "USD");
        var type = TransactionType.Deposit;
        var description = "Test transaction";

        // Act
        var transaction = TransactionEntity.Create(accountId, amount, type, description);

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(accountId, transaction.AccountId);
        Assert.Equal(amount.Amount, transaction.Amount);
        Assert.Equal(type, transaction.Type);
        Assert.Equal(description, transaction.Description);
        Assert.True(transaction.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void CreateTransaction_WithNegativeAmount_ShouldThrowException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = new Money(-100m, "USD");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            TransactionEntity.Create(accountId, amount, TransactionType.Deposit, "Test"));

        Assert.Contains("Amount must be positive", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateTransaction_WithInvalidDescription_ShouldThrowException(string description)
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = new Money(100m, "USD");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            TransactionEntity.Create(accountId, amount, TransactionType.Deposit, description));

        Assert.Contains("Description cannot be empty", exception.Message);
    }
}
```

## 📊 Monitoring & Observability

### Key Metrics

- Transaction processing rate (TPS)
- Transaction success/failure rates
- Average processing time
- Fraud detection alerts
- Daily transaction volumes by type

### Logging Events

```csharp
public static class TransactionEvents
{
    public static readonly EventId TransactionStarted = new(2001, "TransactionStarted");
    public static readonly EventId TransactionCompleted = new(2002, "TransactionCompleted");
    public static readonly EventId TransactionFailed = new(2003, "TransactionFailed");
    public static readonly EventId FraudDetected = new(2004, "FraudDetected");
    public static readonly EventId DuplicateTransaction = new(2005, "DuplicateTransaction");
}
```

### Health Checks

- Database connectivity
- Service Bus connectivity
- Fraud detection service availability
- Account Service communication

## 🚀 Performance Considerations

### Optimization Strategies

- **Connection Pooling**: Efficient database connections
- **Batch Processing**: Group similar transactions
- **Caching**: Cache frequently accessed data
- **Async Processing**: Non-blocking transaction handling
- **Database Indexing**: Optimize query performance

### Scalability Features

- **Horizontal Scaling**: Multiple service instances
- **Database Sharding**: Partition by account ID
- **Event Sourcing**: Replay transactions for consistency
- **Circuit Breaker**: Handle downstream service failures

## 📚 Implementation Status

🚧 **This service is planned for implementation**

Key components to implement:

- [ ] Transaction domain entities and value objects
- [ ] CQRS command handlers with validation
- [ ] Event publishing and handling
- [ ] Fraud detection services
- [ ] API controllers with proper error handling
- [ ] Database context and repositories
- [ ] Comprehensive testing suite
- [ ] Performance monitoring and logging

## 🤝 Contributing

When implementing this service, ensure:

1. Follow CQRS pattern strictly (commands only)
2. Implement proper event sourcing
3. Handle idempotency correctly
4. Include comprehensive fraud detection
5. Maintain transaction audit trails
6. Implement proper error handling and rollback

## 📖 Related Documentation

- [Account Service](../Account/README.md) - For balance updates
- [Movement Service](../Movement/README.md) - For transaction history
- [Security Service](../Security/README.md) - For user authentication
- [CQRS Pattern Documentation](../../docs/cqrs-implementation.md)
