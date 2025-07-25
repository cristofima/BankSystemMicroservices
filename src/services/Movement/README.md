# Movement Service

## Overview

The Movement Service is responsible for managing and tracking all financial movements (transactions) within the Bank System Microservices architecture. It provides comprehensive transaction management, real-time processing, audit trails, and ensures compliance with banking regulations and security standards.

## Service Responsibilities

### What the Movement Service SHOULD Do:

1. **Transaction Processing**

   - Process deposits, withdrawals, and transfers
   - Validate transaction requests against business rules
   - Ensure transaction atomicity and consistency
   - Handle transaction failures and rollbacks

2. **Transaction History Management**

   - Maintain complete transaction history
   - Provide transaction search and filtering capabilities
   - Generate transaction reports and statements
   - Ensure audit trail compliance

3. **Real-time Transaction Processing**

   - Process transactions in real-time
   - Provide immediate transaction status updates
   - Handle concurrent transaction requests
   - Manage transaction queuing and processing

4. **Balance Management**

   - Calculate and maintain account balances
   - Ensure balance accuracy across all transactions
   - Handle balance inquiries and updates
   - Provide real-time balance information

5. **Compliance and Auditing**

   - Maintain detailed audit logs for all transactions
   - Ensure regulatory compliance (AML, KYC)
   - Generate compliance reports
   - Track suspicious transaction patterns

6. **Transaction Validation**
   - Validate transaction amounts and limits
   - Check account status and permissions
   - Enforce daily/monthly transaction limits
   - Validate currency and exchange rates

### What the Movement Service SHOULD NOT Do:

1. **Account Management**

   - Should not create, modify, or delete accounts
   - Should not manage account details or customer information
   - Should not handle account opening/closing processes

2. **User Authentication/Authorization**

   - Should not authenticate users or manage sessions
   - Should not handle password management or security tokens
   - Should rely on Security Service for authentication

3. **Customer Management**

   - Should not manage customer profiles or personal information
   - Should not handle customer onboarding processes
   - Should communicate with Account Service for customer-related data

4. **Notification Services**

   - Should not send emails, SMS, or push notifications directly
   - Should publish events for Notification Service to handle
   - Should not manage communication preferences

5. **Reporting and Analytics**
   - Should not generate complex business reports
   - Should not perform data analytics or business intelligence
   - Should provide data to Reporting Service for analysis

## Service Communication

### Inbound Communications (What calls Movement Service):

1. **Account Service**

   - Requests transaction processing for account operations
   - Queries transaction history for account statements
   - Requests balance information for account inquiries

2. **External Payment Gateways**

   - Sends transaction requests from external sources
   - Provides transaction status updates
   - Handles payment confirmations

3. **API Gateway/Client Applications**
   - Direct transaction requests from mobile/web applications
   - Balance inquiry requests
   - Transaction history requests

### Outbound Communications (What Movement Service calls):

1. **Account Service**

   - Validates account existence and status
   - Retrieves account information for transaction processing
   - Updates account metadata (last transaction date, etc.)

2. **Security Service**

   - Validates JWT tokens for authentication
   - Performs authorization checks for transactions
   - Logs security events for suspicious activities

3. **Notification Service (via Events)**

   - Publishes TransactionProcessed events
   - Sends LimitExceeded events for compliance
   - Publishes SuspiciousActivity events for monitoring

4. **Reporting Service (via Events)**
   - Sends TransactionCompleted events for analytics
   - Provides real-time transaction data
   - Publishes compliance-related events

### Event-Driven Communication:

**Published Events:**

- `TransactionProcessed` - When a transaction is completed
- `TransactionFailed` - When a transaction fails
- `LimitExceeded` - When transaction limits are breached
- `SuspiciousActivity` - When suspicious patterns are detected
- `BalanceUpdated` - When account balance changes
- `ComplianceAlert` - For regulatory compliance issues

**Subscribed Events:**

- `AccountStatusChanged` - From Account Service
- `AccountClosed` - From Account Service
- `SecurityAlert` - From Security Service
- `ComplianceRuleUpdated` - From configuration services

## Architecture

### Clean Architecture Layers

```
Movement.Api/              # Presentation Layer
├── Controllers/           # API Controllers
├── Middleware/           # Query middleware
├── Extensions/           # Service extensions
└── Program.cs           # Application startup

Movement.Application/      # Application Layer
├── Queries/             # CQRS Queries (GetMovements, GetStatement, GetSummary)
├── Handlers/           # Query Handlers
├── DTOs/              # Data Transfer Objects
├── Interfaces/        # Application Interfaces
├── Validators/        # Query parameter validators
└── Mappers/          # AutoMapper Profiles

Movement.Domain/           # Domain Layer
├── Entities/            # Read Model Entities (Movement, Statement)
├── ValueObjects/       # Value Objects (DateRange, MovementSummary)
├── Enums/            # Domain Enumerations (MovementType, StatementPeriod)
└── Exceptions/       # Domain Exceptions

Movement.Infrastructure/   # Infrastructure Layer
├── Data/              # EF Core DbContext (Read-optimized)
├── Repositories/      # Repository Implementations
├── EventHandlers/     # Event Handlers from Transaction Service
├── Services/          # External Service Integrations
└── Reporting/        # Report generation services
```

## 🎯 Service Overview

### Responsibilities

- **Transaction History**: Maintain comprehensive transaction movement records
- **Account Statements**: Generate account statements and summaries
- **Financial Reporting**: Provide data for reports and analytics
- **Movement Queries**: Handle all read operations for transaction data
- **Data Aggregation**: Create summarized views for dashboards

### Domain Boundaries

- Transaction movement history
- Account statement generation
- Financial reporting and analytics
- Read-optimized data models

## 🔧 Features

### Query Capabilities

- **Movement History**: Paginated transaction history with filtering
- **Account Statements**: Monthly, quarterly, and yearly statements
- **Balance Tracking**: Historical balance information
- **Search Functionality**: Advanced filtering by date, amount, type, reference

### Reporting Features

- **PDF Statements**: Generate downloadable account statements
- **CSV Export**: Export transaction data for external analysis
- **Summary Reports**: Daily, weekly, monthly transaction summaries
- **Analytics Data**: Spending patterns and category analysis

### Performance Optimization

- **Read Models**: Denormalized data for fast queries
- **Caching**: Redis caching for frequently accessed data
- **Indexing**: Optimized database indexes for common queries
- **Pagination**: Efficient handling of large result sets

## 🔌 API Endpoints

### Movement Query Endpoints

#### GET /api/movements/account/{accountId}

Get movement history for an account with filtering and pagination.

**Query Parameters:**

- `fromDate`: Start date filter (optional)
- `toDate`: End date filter (optional)
- `type`: Movement type filter (optional)
- `minAmount`: Minimum amount filter (optional)
- `maxAmount`: Maximum amount filter (optional)
- `searchText`: Text search in description/reference (optional)
- `page`: Page number (default: 1)
- `pageSize`: Page size (default: 50, max: 100)
- `sortBy`: Sort field (date, amount, type)
- `sortOrder`: Sort order (asc, desc)

**Response:**

```json
{
  "data": [
    {
      "id": "guid",
      "transactionId": "guid",
      "accountId": "guid",
      "amount": 500.0,
      "type": "Deposit",
      "description": "Salary deposit",
      "reference": "SAL-20240115-001",
      "timestamp": "2024-01-15T10:30:00Z",
      "balanceAfter": 2000.0,
      "category": "Income"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 50,
    "totalPages": 5,
    "totalRecords": 237,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "summary": {
    "totalCredits": 15000.0,
    "totalDebits": 8500.0,
    "netAmount": 6500.0,
    "transactionCount": 237
  }
}
```

#### GET /api/movements/account/{accountId}/statement

Generate account statement for a specific period.

**Query Parameters:**

- `year`: Statement year
- `month`: Statement month (optional, for monthly statements)
- `quarter`: Statement quarter (optional, for quarterly statements)
- `format`: Response format (json, pdf)

**Response (JSON):**

```json
{
  "accountId": "guid",
  "accountNumber": "1234567890",
  "statementPeriod": {
    "startDate": "2024-01-01T00:00:00Z",
    "endDate": "2024-01-31T23:59:59Z",
    "periodType": "Monthly"
  },
  "openingBalance": 1500.0,
  "closingBalance": 2000.0,
  "movements": [
    {
      "date": "2024-01-15",
      "description": "Salary deposit",
      "reference": "SAL-20240115-001",
      "debit": null,
      "credit": 500.0,
      "balance": 2000.0
    }
  ],
  "summary": {
    "totalCredits": 1500.0,
    "totalDebits": 1000.0,
    "netChange": 500.0,
    "averageBalance": 1750.0,
    "transactionCount": 15
  }
}
```

#### GET /api/movements/account/{accountId}/summary

Get movement summary for an account.

**Query Parameters:**

- `period`: Summary period (daily, weekly, monthly, yearly)
- `fromDate`: Start date
- `toDate`: End date
- `groupBy`: Group by field (type, category, month)

#### GET /api/movements/search

Advanced search across movements.

**Query Parameters:**

- `accountIds`: Array of account IDs (optional)
- `fromDate`: Start date filter
- `toDate`: End date filter
- `minAmount`: Minimum amount
- `maxAmount`: Maximum amount
- `types`: Array of movement types
- `searchText`: Text search
- `page`: Page number
- `pageSize`: Page size

#### GET /api/movements/{movementId}

Get specific movement details.

## 🗄️ Data Model

### Movement Entity (Read Model)

```csharp
public class Movement : EntityBase<Guid>
{
    public Guid TransactionId { get; set; }
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public MovementType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Tags { get; set; } // JSON array for additional metadata

    // Denormalized fields for performance
    public string CustomerName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int DayOfYear { get; set; }
}
```

### Statement Models

```csharp
public class AccountStatement
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public StatementPeriod Period { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<StatementMovement> Movements { get; set; } = new();
    public StatementSummary Summary { get; set; }
}

public record StatementPeriod(
    DateTime StartDate,
    DateTime EndDate,
    StatementPeriodType PeriodType);

public record StatementSummary(
    decimal TotalCredits,
    decimal TotalDebits,
    decimal NetChange,
    decimal AverageBalance,
    int TransactionCount);
```

### Value Objects

```csharp
public record DateRange(DateTime StartDate, DateTime EndDate)
{
    public bool Contains(DateTime date) => date >= StartDate && date <= EndDate;
    public TimeSpan Duration => EndDate - StartDate;
}

public record MovementFilter(
    Guid? AccountId = null,
    DateRange? DateRange = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    MovementType? Type = null,
    string? SearchText = null);
```

## ⚙️ Event Handling

### Event Subscribers

The Movement Service subscribes to events from the Transaction Service:

```csharp
public class TransactionCreatedEventHandler : IEventHandler<TransactionCreatedEvent>
{
    public async Task HandleAsync(TransactionCreatedEvent @event, CancellationToken cancellationToken)
    {
        var movement = new Movement
        {
            Id = Guid.NewGuid(),
            TransactionId = @event.TransactionId,
            AccountId = @event.AccountId,
            Amount = @event.Amount.Amount,
            Currency = @event.Amount.Currency,
            Type = MapTransactionType(@event.Type),
            Description = @event.Description,
            Reference = @event.Reference,
            Timestamp = @event.Timestamp,
            Year = @event.Timestamp.Year,
            Month = @event.Timestamp.Month,
            DayOfYear = @event.Timestamp.DayOfYear
        };

        // Get updated balance from Account Service or calculate
        movement.BalanceAfter = await GetAccountBalanceAfterTransaction(@event);

        await _movementRepository.AddAsync(movement, cancellationToken);

        // Update cached summaries
        await _cacheService.InvalidateAccountSummariesAsync(@event.AccountId);
    }
}
```

## 🔍 Query Optimization

### Database Indexes

```sql
-- Primary indexes for common queries
CREATE INDEX IX_Movement_AccountId_Timestamp ON Movements (AccountId, Timestamp DESC);
CREATE INDEX IX_Movement_Year_Month ON Movements (Year, Month, AccountId);
CREATE INDEX IX_Movement_Type_Timestamp ON Movements (Type, Timestamp DESC);
CREATE INDEX IX_Movement_Amount ON Movements (Amount);
CREATE INDEX IX_Movement_Reference ON Movements (Reference);

-- Composite indexes for filtered queries
CREATE INDEX IX_Movement_AccountId_Type_Timestamp ON Movements (AccountId, Type, Timestamp DESC);
CREATE INDEX IX_Movement_AccountId_Year_Month ON Movements (AccountId, Year, Month);
```

### Caching Strategy

```csharp
public class CachedMovementService : IMovementService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IMovementService _baseService;

    public async Task<PagedResult<MovementDto>> GetMovementsAsync(
        MovementQuery query, CancellationToken cancellationToken)
    {
        // Cache key based on query parameters
        var cacheKey = $"movements:{query.AccountId}:{query.GetHashCode()}";

        // Try memory cache first
        if (_memoryCache.TryGetValue(cacheKey, out PagedResult<MovementDto> cached))
            return cached;

        // Try distributed cache
        var distributedCached = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
        if (distributedCached != null)
        {
            var result = JsonSerializer.Deserialize<PagedResult<MovementDto>>(distributedCached);
            _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        // Get from database
        var movements = await _baseService.GetMovementsAsync(query, cancellationToken);

        // Cache the result
        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(movements),
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(15) },
            cancellationToken);

        _memoryCache.Set(cacheKey, movements, TimeSpan.FromMinutes(5));

        return movements;
    }
}
```

## 📊 Reporting Services

### PDF Statement Generation

```csharp
public class PdfStatementService : IStatementService
{
    public async Task<byte[]> GeneratePdfStatementAsync(
        Guid accountId, StatementPeriod period, CancellationToken cancellationToken)
    {
        var statement = await GetStatementDataAsync(accountId, period, cancellationToken);

        using var document = new PdfDocument();
        var page = document.AddPage();
        var graphics = XGraphics.FromPdfPage(page);

        // Header
        DrawStatementHeader(graphics, statement);

        // Account information
        DrawAccountInfo(graphics, statement);

        // Transaction table
        DrawTransactionTable(graphics, statement.Movements);

        // Summary
        DrawSummary(graphics, statement.Summary);

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }
}
```

### CSV Export Service

```csharp
public class CsvExportService : IExportService
{
    public async Task<Stream> ExportMovementsToCsvAsync(
        MovementQuery query, CancellationToken cancellationToken)
    {
        var movements = await _movementService.GetAllMovementsAsync(query, cancellationToken);

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // Write headers
        csv.WriteHeader<MovementCsvRecord>();
        csv.NextRecord();

        // Write data
        foreach (var movement in movements)
        {
            csv.WriteRecord(new MovementCsvRecord
            {
                Date = movement.Timestamp.ToString("yyyy-MM-dd"),
                Description = movement.Description,
                Reference = movement.Reference,
                Amount = movement.Amount,
                Type = movement.Type.ToString(),
                Balance = movement.BalanceAfter
            });
            csv.NextRecord();
        }

        var content = writer.ToString();
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
```

## 🧪 Testing Strategy

### Query Handler Tests

```csharp
public class GetMovementsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ValidQuery_ShouldReturnPagedMovements()
    {
        // Arrange
        var query = new GetMovementsQuery
        {
            AccountId = _testAccountId,
            PageNumber = 1,
            PageSize = 10,
            FromDate = DateTime.UtcNow.AddDays(-30)
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Data.Count <= 10);
        Assert.Equal(1, result.Pagination.CurrentPage);
    }

    [Fact]
    public async Task Handle_FilterByAmount_ShouldReturnFilteredResults()
    {
        // Test amount filtering
    }

    [Fact]
    public async Task Handle_SearchByText_ShouldReturnMatchingResults()
    {
        // Test text search functionality
    }
}
```

### Domain Tests

### Core Entity Tests

```csharp
public class TransferTests
{
    [Fact]
    public void CreateTransfer_ValidData_ShouldCreateSuccessfully()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();
        var amount = new Money(100m, Currency.USD);
        var description = "Transfer to savings";

        // Act
        var transfer = Transfer.Create(sourceAccountId, destinationAccountId, amount, description);

        // Assert
        Assert.NotNull(transfer);
        Assert.Equal(sourceAccountId, transfer.SourceAccountId);
        Assert.Equal(destinationAccountId, transfer.DestinationAccountId);
        Assert.Equal(amount, transfer.Amount);
        Assert.Equal(description, transfer.Description);
        Assert.Equal(TransferStatus.Pending, transfer.Status);
    }

    [Fact]
    public void CreateTransfer_SameAccount_ShouldThrowException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = new Money(100m, Currency.USD);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Transfer.Create(accountId, accountId, amount, "Invalid transfer"));
    }

    [Fact]
    public void CreateTransfer_ZeroAmount_ShouldThrowException()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();
        var amount = new Money(0m, Currency.USD);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Transfer.Create(sourceAccountId, destinationAccountId, amount, "Zero amount"));
    }

    [Fact]
    public void MarkAsCompleted_PendingTransfer_ShouldUpdateStatus()
    {
        // Arrange
        var transfer = CreateValidTransfer();

        // Act
        transfer.MarkAsCompleted();

        // Assert
        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.True(transfer.CompletedAt.HasValue);
    }

    [Fact]
    public void MarkAsFailed_PendingTransfer_ShouldUpdateStatusWithReason()
    {
        // Arrange
        var transfer = CreateValidTransfer();
        var failureReason = "Insufficient funds";

        // Act
        transfer.MarkAsFailed(failureReason);

        // Assert
        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.Equal(failureReason, transfer.FailureReason);
    }

    private static Transfer CreateValidTransfer()
    {
        return Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(100m, Currency.USD),
            "Test transfer");
    }
}
```

## Application Tests

### Command Handler Tests

```csharp
public class CreateTransferCommandHandlerTests
{
    private readonly Mock<ITransferRepository> _mockTransferRepository;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<CreateTransferCommandHandler>> _mockLogger;
    private readonly CreateTransferCommandHandler _handler;

    public CreateTransferCommandHandlerTests()
    {
        _mockTransferRepository = new Mock<ITransferRepository>();
        _mockAccountService = new Mock<IAccountService>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<CreateTransferCommandHandler>>();

        _handler = new CreateTransferCommandHandler(
            _mockTransferRepository.Object,
            _mockAccountService.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateTransfer()
    {
        // Arrange
        var command = new CreateTransferCommand
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Description = "Test transfer"
        };

        _mockAccountService.Setup(x => x.AccountExistsAsync(command.SourceAccountId))
            .ReturnsAsync(true);
        _mockAccountService.Setup(x => x.AccountExistsAsync(command.DestinationAccountId))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        _mockTransferRepository.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<TransferCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentSourceAccount_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateTransferCommand
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Description = "Test transfer"
        };

        _mockAccountService.Setup(x => x.AccountExistsAsync(command.SourceAccountId))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Source account not found", result.Error);
        _mockTransferRepository.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonExistentDestinationAccount_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateTransferCommand
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Description = "Test transfer"
        };

        _mockAccountService.Setup(x => x.AccountExistsAsync(command.SourceAccountId))
            .ReturnsAsync(true);
        _mockAccountService.Setup(x => x.AccountExistsAsync(command.DestinationAccountId))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Destination account not found", result.Error);
        _mockTransferRepository.Verify(x => x.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

### Query Handler Tests

```csharp
public class GetTransferQueryHandlerTests
{
    private readonly Mock<ITransferRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GetTransferQueryHandler _handler;

    public GetTransferQueryHandlerTests()
    {
        _mockRepository = new Mock<ITransferRepository>();
        _mockMapper = new Mock<IMapper>();
        _handler = new GetTransferQueryHandler(_mockRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_ExistingTransfer_ShouldReturnTransferDto()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var query = new GetTransferQuery(transferId);
        var transfer = CreateValidTransfer();
        var transferDto = new TransferDto { Id = transferId };

        _mockRepository.Setup(x => x.GetByIdAsync(transferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transfer);
        _mockMapper.Setup(x => x.Map<TransferDto>(transfer))
            .Returns(transferDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(transferDto, result.Value);
    }

    [Fact]
    public async Task Handle_NonExistentTransfer_ShouldReturnFailure()
    {
        // Arrange
        var transferId = Guid.NewGuid();
        var query = new GetTransferQuery(transferId);

        _mockRepository.Setup(x => x.GetByIdAsync(transferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transfer)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Transfer not found", result.Error);
    }

    private static Transfer CreateValidTransfer()
    {
        return Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(100m, Currency.USD),
            "Test transfer");
    }
}
```

## Integration Tests

### API Controller Tests

```csharp
public class TransferControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransferControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransfer_ValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD",
            Description = "Integration test transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transfers", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TransferDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.Equal(request.SourceAccountId, result.SourceAccountId);
        Assert.Equal(request.DestinationAccountId, result.DestinationAccountId);
        Assert.Equal(request.Amount, result.Amount);
    }

    [Fact]
    public async Task CreateTransfer_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTransferRequest
        {
            SourceAccountId = Guid.Empty, // Invalid
            DestinationAccountId = Guid.NewGuid(),
            Amount = -100m, // Invalid
            Currency = "USD",
            Description = "Invalid transfer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transfers", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTransfer_ExistingId_ShouldReturnTransfer()
    {
        // Arrange
        var transferId = await CreateTestTransferAsync();

        // Act
        var response = await _client.GetAsync($"/api/transfers/{transferId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TransferDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.Equal(transferId, result.Id);
    }

    [Fact]
    public async Task GetTransfer_NonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/transfers/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateTestTransferAsync()
    {
        var request = new CreateTransferRequest
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 50m,
            Currency = "USD",
            Description = "Test transfer for retrieval"
        };

        var response = await _client.PostAsJsonAsync("/api/transfers", request);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TransferDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result.Id;
    }
}
```

### Event Publisher Tests

```csharp
public class TransferEventPublisherTests
{
    private readonly Mock<IServiceBusClient> _mockServiceBusClient;
    private readonly Mock<ILogger<TransferEventPublisher>> _mockLogger;
    private readonly TransferEventPublisher _eventPublisher;

    public TransferEventPublisherTests()
    {
        _mockServiceBusClient = new Mock<IServiceBusClient>();
        _mockLogger = new Mock<ILogger<TransferEventPublisher>>();
        _eventPublisher = new TransferEventPublisher(_mockServiceBusClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PublishAsync_TransferCreatedEvent_ShouldSendMessage()
    {
        // Arrange
        var transferEvent = new TransferCreatedEvent
        {
            TransferId = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "USD"
        };

        var mockSender = new Mock<ServiceBusSender>();
        _mockServiceBusClient.Setup(x => x.CreateSender("transfer-events"))
            .Returns(mockSender.Object);

        // Act
        await _eventPublisher.PublishAsync(transferEvent, CancellationToken.None);

        // Assert
        mockSender.Verify(x => x.SendMessageAsync(
            It.Is<ServiceBusMessage>(m => m.Subject == "TransferCreated"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_TransferCompletedEvent_ShouldSendMessage()
    {
        // Arrange
        var transferEvent = new TransferCompletedEvent
        {
            TransferId = Guid.NewGuid(),
            CompletedAt = DateTime.UtcNow
        };

        var mockSender = new Mock<ServiceBusSender>();
        _mockServiceBusClient.Setup(x => x.CreateSender("transfer-events"))
            .Returns(mockSender.Object);

        // Act
        await _eventPublisher.PublishAsync(transferEvent, CancellationToken.None);

        // Assert
        mockSender.Verify(x => x.SendMessageAsync(
            It.Is<ServiceBusMessage>(m => m.Subject == "TransferCompleted"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## 📈 Performance Monitoring

### Key Metrics

- Query response times
- Cache hit/miss ratios
- Database query performance
- Memory usage for large result sets
- Statement generation times

### Health Checks

- Database connectivity
- Cache availability (Redis)
- Event subscription health
- Memory usage thresholds

## 🗄️ Database Optimization

### Read-Optimized Schema

```sql
-- Denormalized movement table for fast queries
CREATE TABLE Movements (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TransactionId UNIQUEIDENTIFIER NOT NULL,
    AccountId UNIQUEIDENTIFIER NOT NULL,
    AccountNumber NVARCHAR(20) NOT NULL, -- Denormalized
    Amount DECIMAL(18,2) NOT NULL,
    Currency NVARCHAR(3) NOT NULL,
    Type INT NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Reference NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50) NULL,
    Timestamp DATETIME2 NOT NULL,
    BalanceAfter DECIMAL(18,2) NOT NULL,
    CustomerName NVARCHAR(200) NOT NULL, -- Denormalized
    Year INT NOT NULL, -- Pre-calculated for fast filtering
    Month INT NOT NULL, -- Pre-calculated for fast filtering
    DayOfYear INT NOT NULL, -- Pre-calculated for fast filtering
    Tags NVARCHAR(MAX) NULL, -- JSON field
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Summary tables for fast aggregations
CREATE TABLE DailySummaries (
    AccountId UNIQUEIDENTIFIER,
    Date DATE,
    TotalCredits DECIMAL(18,2),
    TotalDebits DECIMAL(18,2),
    TransactionCount INT,
    EndingBalance DECIMAL(18,2),
    PRIMARY KEY (AccountId, Date)
);
```

## 📚 Implementation Status

🚧 **This service is planned for implementation**

Key components to implement:

- [ ] Read model entities and value objects
- [ ] CQRS query handlers with caching
- [ ] Event handlers for transaction events
- [ ] Advanced filtering and search capabilities
- [ ] Statement generation services (PDF, CSV)
- [ ] API controllers with comprehensive querying
- [ ] Database context optimized for reads
- [ ] Caching layer implementation
- [ ] Performance monitoring and metrics

## 🤝 Contributing

When implementing this service, ensure:

1. Focus on query performance and optimization
2. Implement comprehensive caching strategies
3. Design for read-heavy workloads
4. Handle event processing idempotently
5. Provide flexible querying capabilities
6. Include proper error handling for large datasets

## 📖 Related Documentation

- [Transaction Service](../Transaction/README.md) - Source of transaction events
- [Account Service](../Account/README.md) - For account information
- [CQRS Query Patterns](../../docs/cqrs-query-patterns.md)
- [Performance Optimization Guide](../../docs/performance-optimization.md)
