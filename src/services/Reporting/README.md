# Reporting Service

## Overview

The Reporting Service is responsible for generating comprehensive financial reports and analytics for the Bank System Microservices architecture. It provides business intelligence capabilities, regulatory compliance reporting, and data analytics for business decision-making.

## Service Responsibilities

### What This Service DOES

- **Financial Report Generation**: Creates account statements, transaction summaries, and balance reports
- **Regulatory Compliance Reporting**: Generates reports required by banking regulations (AML, KYC, etc.)
- **Business Analytics**: Provides insights on customer behavior, transaction patterns, and business metrics
- **Data Aggregation**: Consolidates data from multiple services for comprehensive reporting
- **Scheduled Report Generation**: Automatically generates periodic reports (daily, weekly, monthly)
- **Report Export**: Supports multiple formats (PDF, Excel, CSV, JSON)
- **Performance Metrics**: Tracks and reports on system performance and business KPIs
- **Audit Trail Reports**: Generates audit logs and compliance documentation

### What This Service DOES NOT DO

- **Data Modification**: Does not create, update, or delete business entities
- **User Authentication**: Does not handle user login or security (delegates to Security Service)
- **Real-time Transaction Processing**: Does not process financial transactions
- **Account Management**: Does not create or modify accounts
- **Payment Processing**: Does not handle payment execution
- **Notification Sending**: Does not send reports (delegates to Notification Service)

## Architecture

This service follows Clean Architecture principles with the following layers:

- **API Layer**: REST controllers for report requests and status checking
- **Application Layer**: Report generation commands, queries, and business logic
- **Domain Layer**: Report entities, value objects, and business rules
- **Infrastructure Layer**: Data access, external integrations, and report storage

## Data Management

### Read-Only Data Sources

The Reporting Service maintains read-only views of data from other services:

```csharp
// Account data (read-only)
public class AccountSummary
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public AccountStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Transaction data (read-only)
public class TransactionSummary
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string ReferenceNumber { get; set; }
}

// Movement data (read-only)
public class MovementSummary
{
    public Guid Id { get; set; }
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public MovementType Type { get; set; }
    public MovementStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}
```

### Report Entities

```csharp
// Report definition
public class ReportDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ReportType Type { get; set; }
    public string Parameters { get; set; } // JSON
    public string Template { get; set; }
    public bool IsActive { get; set; }
    public ReportSchedule Schedule { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Report execution
public class ReportExecution
{
    public Guid Id { get; set; }
    public Guid ReportDefinitionId { get; set; }
    public string Parameters { get; set; } // JSON
    public ReportStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string FilePath { get; set; }
    public string ErrorMessage { get; set; }
    public string RequestedBy { get; set; }
}
```

## Events

### Events This Service PUBLISHES

```csharp
// Report completed event
public record ReportGeneratedEvent(
    Guid ReportExecutionId,
    Guid ReportDefinitionId,
    string ReportName,
    ReportStatus Status,
    string FilePath,
    string RequestedBy,
    DateTime CompletedAt) : IDomainEvent;

// Report failed event
public record ReportGenerationFailedEvent(
    Guid ReportExecutionId,
    Guid ReportDefinitionId,
    string ReportName,
    string ErrorMessage,
    string RequestedBy,
    DateTime FailedAt) : IDomainEvent;

// Scheduled report event
public record ScheduledReportTriggeredEvent(
    Guid ReportDefinitionId,
    string ReportName,
    ReportSchedule Schedule,
    DateTime TriggeredAt) : IDomainEvent;
```

### Events This Service CONSUMES

```csharp
// From Account Service
public record AccountCreatedEvent(
    Guid AccountId,
    string AccountNumber,
    Guid CustomerId,
    decimal InitialBalance,
    DateTime CreatedAt) : IDomainEvent;

public record AccountStatusChangedEvent(
    Guid AccountId,
    AccountStatus OldStatus,
    AccountStatus NewStatus,
    string Reason,
    DateTime ChangedAt) : IDomainEvent;

// From Transaction Service
public record TransactionProcessedEvent(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    TransactionType Type,
    string Description,
    DateTime ProcessedAt) : IDomainEvent;

// From Movement Service
public record MovementProcessedEvent(
    Guid MovementId,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    MovementType Type,
    DateTime ProcessedAt) : IDomainEvent;

// From Security Service
public record UserLoggedInEvent(
    string UserId,
    string Email,
    DateTime LoginTime,
    string IpAddress) : IDomainEvent;
```

## API Endpoints

### Report Management

```csharp
// Generate on-demand report
[HttpPost("reports/generate")]
public async Task<ActionResult<ReportExecutionDto>> GenerateReport(
    [FromBody] GenerateReportRequest request)
{
    var command = new GenerateReportCommand(
        request.ReportDefinitionId,
        request.Parameters,
        request.Format,
        User.Identity.Name);

    var result = await _mediator.Send(command);
    return result.IsSuccess
        ? Accepted(result.Value)
        : BadRequest(result.Error);
}

// Get report status
[HttpGet("reports/executions/{executionId}")]
public async Task<ActionResult<ReportExecutionDto>> GetReportExecution(Guid executionId)
{
    var query = new GetReportExecutionQuery(executionId);
    var result = await _mediator.Send(query);

    return result.IsSuccess
        ? Ok(result.Value)
        : NotFound();
}

// Download report
[HttpGet("reports/executions/{executionId}/download")]
public async Task<IActionResult> DownloadReport(Guid executionId)
{
    var query = new DownloadReportQuery(executionId);
    var result = await _mediator.Send(query);

    if (!result.IsSuccess)
        return NotFound();

    return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
}

// List available reports
[HttpGet("reports/definitions")]
public async Task<ActionResult<PagedResult<ReportDefinitionDto>>> GetReportDefinitions(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
{
    var query = new GetReportDefinitionsQuery(page, pageSize);
    var result = await _mediator.Send(query);

    return Ok(result);
}
```

### Analytics Endpoints

```csharp
// Account analytics
[HttpGet("analytics/accounts")]
public async Task<ActionResult<AccountAnalyticsDto>> GetAccountAnalytics(
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null)
{
    var query = new GetAccountAnalyticsQuery(fromDate, toDate);
    var result = await _mediator.Send(query);

    return Ok(result);
}

// Transaction analytics
[HttpGet("analytics/transactions")]
public async Task<ActionResult<TransactionAnalyticsDto>> GetTransactionAnalytics(
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null,
    [FromQuery] string groupBy = "day")
{
    var query = new GetTransactionAnalyticsQuery(fromDate, toDate, groupBy);
    var result = await _mediator.Send(query);

    return Ok(result);
}

// Performance metrics
[HttpGet("analytics/performance")]
public async Task<ActionResult<PerformanceMetricsDto>> GetPerformanceMetrics(
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null)
{
    var query = new GetPerformanceMetricsQuery(fromDate, toDate);
    var result = await _mediator.Send(query);

    return Ok(result);
}
```

## Inter-Service Communication

### Direct Communication

The Reporting Service communicates with other services primarily through:

1. **Read-Only Database Views**: Maintains materialized views of data from other services
2. **Event Sourcing**: Rebuilds state from domain events
3. **Scheduled Data Synchronization**: Periodic updates of reporting data

### Service Dependencies

```csharp
// External service interfaces
public interface IAccountDataService
{
    Task<IEnumerable<AccountSummary>> GetAccountsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);
    Task<AccountSummary> GetAccountAsync(Guid accountId);
}

public interface ITransactionDataService
{
    Task<IEnumerable<TransactionSummary>> GetTransactionsAsync(
        Guid? accountId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
}

public interface IMovementDataService
{
    Task<IEnumerable<MovementSummary>> GetMovementsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);
}
```

## Business Rules

### Report Generation Rules

- Reports can only be generated by authenticated users
- Large reports (>10MB) are processed asynchronously
- Reports are automatically deleted after 30 days
- Maximum of 5 concurrent report generations per user
- Scheduled reports run during off-peak hours (1-5 AM)

### Data Access Rules

- All data access is read-only
- Historical data older than 7 years is archived
- Sensitive data is masked in non-privileged user reports
- Audit logs are maintained for all report accesses

### Performance Rules

- Report generation timeout: 30 minutes
- Maximum result set: 1 million records
- Caching duration: 15 minutes for frequently accessed reports
- Database query timeout: 5 minutes

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BankSystem_Reporting;Trusted_Connection=true;",
    "ReadOnlyConnection": "Server=localhost;Database=BankSystem_ReadReplica;Trusted_Connection=true;"
  },
  "ReportingOptions": {
    "MaxConcurrentReports": 10,
    "ReportTimeoutMinutes": 30,
    "ReportRetentionDays": 30,
    "MaxFileSizeMB": 100,
    "SupportedFormats": ["PDF", "Excel", "CSV", "JSON"]
  },
  "ScheduleOptions": {
    "CheckIntervalMinutes": 5,
    "MaxRetries": 3,
    "RetryDelayMinutes": 10
  },
  "StorageOptions": {
    "ReportStoragePath": "C:\\Reports",
    "TempStoragePath": "C:\\Temp\\Reports",
    "BackupStoragePath": "C:\\Reports\\Backup"
  }
}
```

## Error Handling

### Domain Exceptions

```csharp
public class ReportGenerationException : DomainException
{
    public ReportGenerationException(string message) : base(message) { }
    public ReportGenerationException(string message, Exception innerException) : base(message, innerException) { }
}

public class ReportNotFoundException : DomainException
{
    public ReportNotFoundException(Guid reportId)
        : base($"Report with ID '{reportId}' was not found") { }
}

public class ReportAccessDeniedException : DomainException
{
    public ReportAccessDeniedException(string userId, Guid reportId)
        : base($"User '{userId}' does not have access to report '{reportId}'") { }
}
```

### Global Error Handling

The service uses the same global exception handling middleware as other services, with specific handling for reporting errors.

## Testing

### Unit Testing Examples

```csharp
[Fact]
public async Task GenerateReportCommand_ValidRequest_ShouldReturnSuccess()
{
    // Arrange
    var command = new GenerateReportCommand(
        Guid.NewGuid(),
        "{}",
        ReportFormat.PDF,
        "test@example.com");

    var mockRepository = new Mock<IReportRepository>();
    var mockDataService = new Mock<IAccountDataService>();

    mockRepository.Setup(r => r.GetReportDefinitionAsync(It.IsAny<Guid>()))
        .ReturnsAsync(new ReportDefinition { Id = command.ReportDefinitionId, IsActive = true });

    var handler = new GenerateReportCommandHandler(
        mockRepository.Object,
        mockDataService.Object,
        Mock.Of<ILogger<GenerateReportCommandHandler>>());

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    mockRepository.Verify(r => r.AddExecutionAsync(It.IsAny<ReportExecution>()), Times.Once);
}

[Fact]
public async Task GetReportExecution_NonExistentReport_ShouldReturnNotFound()
{
    // Arrange
    var reportId = Guid.NewGuid();
    var query = new GetReportExecutionQuery(reportId);

    var mockRepository = new Mock<IReportRepository>();
    mockRepository.Setup(r => r.GetExecutionAsync(reportId))
        .ReturnsAsync((ReportExecution)null);

    var handler = new GetReportExecutionQueryHandler(
        mockRepository.Object,
        Mock.Of<IMapper>());

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("not found", result.Error);
}

[Theory]
[InlineData(ReportFormat.PDF)]
[InlineData(ReportFormat.Excel)]
[InlineData(ReportFormat.CSV)]
public async Task ReportGenerator_SupportedFormats_ShouldGenerateSuccessfully(ReportFormat format)
{
    // Arrange
    var generator = new ReportGenerator(Mock.Of<ILogger<ReportGenerator>>());
    var data = new[] { new { Name = "Test", Value = 100 } };

    // Act
    var result = await generator.GenerateAsync(data, format);

    // Assert
    Assert.NotNull(result);
    Assert.True(result.Length > 0);
}
```

### Integration Testing Examples

```csharp
public class ReportingControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReportingControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GenerateReport_ValidRequest_ShouldReturnAccepted()
    {
        // Arrange
        var request = new GenerateReportRequest
        {
            ReportDefinitionId = Guid.NewGuid(),
            Parameters = "{}",
            Format = ReportFormat.PDF
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports/generate", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ReportExecutionDto>(content);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task GetReportDefinitions_ShouldReturnPagedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/reports/definitions?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ReportDefinitionDto>>(content);
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetAccountAnalytics_WithDateRange_ShouldReturnAnalytics()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync(
            $"/api/analytics/accounts?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AccountAnalyticsDto>(content);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DownloadReport_CompletedReport_ShouldReturnFile()
    {
        // Arrange
        var executionId = await CreateCompletedReportAsync();

        // Act
        var response = await _client.GetAsync($"/api/reports/executions/{executionId}/download");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Content.Headers.ContentLength > 0);
    }

    private async Task<Guid> CreateCompletedReportAsync()
    {
        // Helper method to create a completed report for testing
        var request = new GenerateReportRequest
        {
            ReportDefinitionId = Guid.NewGuid(),
            Parameters = "{}",
            Format = ReportFormat.PDF
        };

        var response = await _client.PostAsJsonAsync("/api/reports/generate", request);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ReportExecutionDto>(content);

        // Wait for completion (in real test, you'd mock this)
        await Task.Delay(1000);

        return result.Id;
    }
}
```

## Security

### Authentication and Authorization

- All endpoints require authentication
- Report access is role-based (Admin, Manager, User)
- Sensitive reports require additional permissions
- Report downloads are logged for audit purposes

### Data Security

- Sensitive data is masked in reports based on user roles
- Report files are encrypted at rest
- Temporary files are securely deleted after processing
- Access logs are maintained for compliance

## Monitoring and Logging

### Key Metrics

- Report generation success/failure rates
- Average report generation time
- Report download frequency
- System performance during report generation
- Data synchronization lag

### Health Checks

```csharp
public class ReportingHealthCheck : IHealthCheck
{
    private readonly IReportRepository _repository;
    private readonly IDbConnection _dbConnection;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check database connectivity
            await _dbConnection.QueryAsync("SELECT 1");

            // Check recent report generation success rate
            var recentExecutions = await _repository.GetRecentExecutionsAsync(TimeSpan.FromHours(1));
            var successRate = recentExecutions.Count(e => e.Status == ReportStatus.Completed)
                            / (double)recentExecutions.Count();

            if (successRate < 0.8)
            {
                return HealthCheckResult.Degraded($"Report success rate is {successRate:P}");
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Reporting service is unhealthy", ex);
        }
    }
}
```

## Deployment

### Docker Configuration

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["services/Reporting/src/Reporting.Api/Reporting.Api.csproj", "services/Reporting/src/Reporting.Api/"]
COPY ["services/Reporting/src/Reporting.Application/Reporting.Application.csproj", "services/Reporting/src/Reporting.Application/"]
COPY ["services/Reporting/src/Reporting.Domain/Reporting.Domain.csproj", "services/Reporting/src/Reporting.Domain/"]
COPY ["services/Reporting/src/Reporting.Infrastructure/Reporting.Infrastructure.csproj", "services/Reporting/src/Reporting.Infrastructure/"]

RUN dotnet restore "services/Reporting/src/Reporting.Api/Reporting.Api.csproj"
COPY . .
WORKDIR "/src/services/Reporting/src/Reporting.Api"
RUN dotnet build "Reporting.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Reporting.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Reporting.Api.dll"]
```

### Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=sql-server;Database=BankSystem_Reporting;User Id=reporting_user;Password=***;
ConnectionStrings__ReadOnlyConnection=Server=sql-server-replica;Database=BankSystem_ReadReplica;User Id=readonly_user;Password=***;
ReportingOptions__MaxConcurrentReports=20
ReportingOptions__ReportRetentionDays=90
StorageOptions__ReportStoragePath=/app/reports
AzureServiceBus__ConnectionString=Endpoint=sb://***
```

## Development Guidelines

### Adding New Report Types

1. Create report definition in database
2. Implement report generator class
3. Add corresponding DTO and validation
4. Create unit tests for the new report type
5. Update API documentation

### Performance Considerations

- Use read replicas for data queries
- Implement caching for frequently accessed data
- Use background services for large report generation
- Monitor and optimize database queries
- Implement pagination for large datasets

### Best Practices

- Keep report generation logic separate from data access
- Use domain events for audit trail
- Implement proper error handling and retry mechanisms
- Cache static reference data
- Use streaming for large file downloads

## Contributing

Please see [CONTRIBUTING.md](../../../CONTRIBUTING.md) for development guidelines and coding standards.

## License

This project is licensed under the MIT License - see [LICENSE](../../../LICENSE) for details.
