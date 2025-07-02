# Movement Service

The Movement Service provides transaction history and reporting capabilities within the Bank System Microservices architecture. It implements the Query side of the CQRS pattern, maintaining optimized read models for transaction history, account statements, and financial reporting.

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

## 🏗️ Architecture

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
[TestFixture]
public class GetMovementsQueryHandlerTests
{
    [Test]
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
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Has.Count.LessThanOrEqualTo(10));
        Assert.That(result.Pagination.CurrentPage, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_FilterByAmount_ShouldReturnFilteredResults()
    {
        // Test amount filtering
    }

    [Test]
    public async Task Handle_SearchByText_ShouldReturnMatchingResults()
    {
        // Test text search functionality
    }
}
```

### Integration Tests

```csharp
[TestFixture]
public class MovementControllerTests
{
    [Test]
    public async Task GetMovements_ValidParameters_ShouldReturnOk()
    {
        // Test API endpoints
    }

    [Test]
    public async Task GenerateStatement_ValidPeriod_ShouldReturnPdf()
    {
        // Test statement generation
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
