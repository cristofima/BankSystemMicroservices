# Documentation Standards

## Overview

This document provides comprehensive guidelines for code documentation, XML comments, and inline documentation in the Bank System Microservices project, following .NET documentation best practices.

## XML Documentation Comments

### Basic Structure

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
    // Implementation
}
```

### Class Documentation

```csharp
/// <summary>
/// Service responsible for processing financial transactions within the banking system.
/// Handles deposits, withdrawals, transfers, and fee calculations while ensuring
/// business rules and regulatory compliance.
/// </summary>
/// <remarks>
/// This service implements the transaction processing workflow defined in the
/// Banking Domain Requirements (BDR-001). All transactions are validated against
/// account limits, regulatory requirements, and fraud detection rules.
///
/// <para>
/// Thread Safety: This service is thread-safe and can be used concurrently.
/// All operations are atomic and use optimistic concurrency control.
/// </para>
///
/// <para>
/// Performance: Transaction processing typically completes within 200ms for
/// standard operations. Complex validations may take up to 2 seconds.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var service = new TransactionService(repository, logger);
/// var result = await service.ProcessWithdrawalAsync(
///     accountId: Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
///     amount: 100.00m,
///     description: "ATM Withdrawal");
///
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Transaction completed: {result.Value.Id}");
/// }
/// </code>
/// </example>
public class TransactionService
{
    // Implementation
}
```

### Method Documentation Standards

```csharp
/// <summary>
/// Validates whether a transaction can be processed based on business rules.
/// </summary>
/// <param name="command">The transaction command containing transaction details</param>
/// <returns>
/// A validation result indicating whether the transaction is valid.
/// Returns <c>true</c> if valid, <c>false</c> otherwise with error details.
/// </returns>
/// <remarks>
/// This method performs the following validations:
/// <list type="bullet">
/// <item><description>Account exists and is active</description></item>
/// <item><description>Sufficient funds for withdrawal operations</description></item>
/// <item><description>Daily transaction limits not exceeded</description></item>
/// <item><description>Regulatory compliance checks</description></item>
/// </list>
///
/// <para>
/// Validation rules are configurable through the BusinessRulesOptions configuration.
/// See the Configuration Guide for details on customizing validation parameters.
/// </para>
/// </remarks>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="command"/> is null.
/// </exception>
/// <seealso cref="BusinessRulesOptions"/>
/// <seealso cref="ProcessTransactionAsync"/>
private async Task<ValidationResult> ValidateTransactionAsync(CreateTransactionCommand command)
{
    // Implementation
}
```

### Property Documentation

```csharp
/// <summary>
/// Gets the current balance of the account in the account's base currency.
/// </summary>
/// <value>
/// The current account balance. This value is updated in real-time as
/// transactions are processed and may include pending transactions.
/// </value>
/// <remarks>
/// The balance reflects all completed transactions as of the last update.
/// For real-time balance including pending transactions, use the
/// <see cref="GetAvailableBalanceAsync"/> method.
/// </remarks>
public Money Balance { get; private set; }

/// <summary>
/// Gets or sets the maximum number of retry attempts for failed operations.
/// </summary>
/// <value>
/// The maximum retry count. Default value is 3. Valid range is 1-10.
/// </value>
/// <exception cref="ArgumentOutOfRangeException">
/// Thrown when value is less than 1 or greater than 10.
/// </exception>
public int MaxRetryCount
{
    get => _maxRetryCount;
    set
    {
        if (value < 1 || value > 10)
            throw new ArgumentOutOfRangeException(nameof(value), "Retry count must be between 1 and 10");
        _maxRetryCount = value;
    }
}
```

### Interface Documentation

```csharp
/// <summary>
/// Defines the contract for account repository operations in the banking system.
/// </summary>
/// <remarks>
/// This interface provides data access methods for account entities.
/// Implementations should ensure thread safety and handle connection failures gracefully.
///
/// <para>
/// All async methods accept a CancellationToken to support operation cancellation.
/// Implementations should respect cancellation requests and clean up resources appropriately.
/// </para>
/// </remarks>
public interface IAccountRepository
{
    /// <summary>
    /// Retrieves an account by its unique identifier.
    /// </summary>
    /// <param name="id">The unique account identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>
    /// A task representing the async operation. The task result contains the account
    /// if found, or null if no account exists with the specified identifier.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty.
    /// </exception>
    /// <exception cref="DataAccessException">
    /// Thrown when a database error occurs during the operation.
    /// </exception>
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
```

## Inline Code Comments

### When to Use Comments

```csharp
public async Task<Result<Transaction>> ProcessTransactionAsync(CreateTransactionCommand command)
{
    // Guard clauses - validate input parameters
    if (command.AccountId == Guid.Empty)
        return Result<Transaction>.Failure("Account ID cannot be empty");

    if (command.Amount <= 0)
        return Result<Transaction>.Failure("Amount must be positive");

    // Retrieve account with optimistic locking
    var account = await _accountRepository.GetByIdAsync(command.AccountId, cancellationToken);
    if (account == null)
        throw new AccountNotFoundException(command.AccountId);

    // Complex business rule - explain regulatory requirement
    // Apply daily withdrawal limit check (regulatory requirement SEC-2023-001)
    // This limit is mandated by federal banking regulations and cannot be disabled
    var dailyWithdrawals = await GetDailyWithdrawalsAsync(command.AccountId, cancellationToken);
    if (dailyWithdrawals + command.Amount > DailyWithdrawalLimit)
        return Result<Transaction>.Failure("Daily withdrawal limit exceeded");

    // Execute domain operation - this triggers domain events
    var transactionResult = account.ProcessTransaction(
        new Money(command.Amount, Currency.USD),
        command.Type,
        command.Description);

    if (!transactionResult.IsSuccess)
        return Result<Transaction>.Failure(transactionResult.Error);

    // Persist changes using Unit of Work pattern
    // Order matters: save account first, then transaction
    await _accountRepository.UpdateAsync(account, cancellationToken);
    await _transactionRepository.AddAsync(transactionResult.Value, cancellationToken);

    // Publish domain events for eventual consistency
    await PublishDomainEventsAsync(account.DomainEvents, cancellationToken);
    account.ClearDomainEvents();

    return Result<Transaction>.Success(transactionResult.Value);
}
```

### Comment Best Practices

```csharp
// ✅ Good: Explain WHY, not WHAT
// Use exponential backoff to handle temporary database connection issues
// This prevents overwhelming the database during high load periods
var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt));
await Task.Delay(delay, cancellationToken);

// ✅ Good: Explain business rules and regulatory requirements
// Federal regulation 12 CFR 205.17 requires transaction monitoring
// for amounts exceeding $3,000 to detect potential money laundering
if (transaction.Amount > 3000m)
{
    await _complianceService.FlagForReviewAsync(transaction);
}

// ✅ Good: Explain complex algorithms or calculations
// Calculate compound interest using the formula: A = P(1 + r/n)^(nt)
// Where P = principal, r = annual rate, n = compounds per year, t = time in years
var compoundAmount = principal * Math.Pow(1 + (annualRate / compoundsPerYear),
    compoundsPerYear * timeInYears);

// ✅ Good: Explain workarounds or technical debt
// TODO: Remove this workaround once EF Core issue #12345 is fixed
// Currently, EF Core doesn't support filtered includes with owned types
// so we need to load all transactions and filter in memory
var allTransactions = account.Transactions.ToList();
var recentTransactions = allTransactions
    .Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-30))
    .ToList();

// ❌ Bad: Comments that just repeat the code
// Set the account id
account.Id = Guid.NewGuid();

// ❌ Bad: Obvious comments
// Check if amount is greater than zero
if (amount > 0)
{
    // Process the transaction
    ProcessTransaction(amount);
}
```

### Complex Logic Documentation

```csharp
/// <summary>
/// Calculates the risk score for a transaction using a proprietary algorithm.
/// </summary>
/// <remarks>
/// Risk Score Calculation Algorithm:
///
/// The risk score is calculated using multiple factors weighted by importance:
///
/// 1. Amount Factor (30% weight):
///    - Transactions under $100: Score = 0
///    - Transactions $100-$1000: Score = Amount / 100
///    - Transactions over $1000: Score = 10 + (Amount - 1000) / 1000
///
/// 2. Frequency Factor (25% weight):
///    - Based on transactions in last 24 hours
///    - Score = min(TransactionCount * 2, 20)
///
/// 3. Location Factor (20% weight):
///    - Same city as registered address: Score = 0
///    - Same country: Score = 5
///    - Different country: Score = 15
///
/// 4. Time Factor (15% weight):
///    - Business hours (9 AM - 5 PM): Score = 0
///    - Evening (5 PM - 11 PM): Score = 3
///    - Night (11 PM - 9 AM): Score = 8
///
/// 5. Account Age Factor (10% weight):
///    - Over 1 year: Score = 0
///    - 6 months - 1 year: Score = 2
///    - Under 6 months: Score = 5
///
/// Final Score = (AmountScore * 0.3) + (FrequencyScore * 0.25) +
///               (LocationScore * 0.2) + (TimeScore * 0.15) + (AgeScore * 0.1)
///
/// Risk Levels:
/// - Low Risk: 0-10
/// - Medium Risk: 11-25
/// - High Risk: 26-50
/// - Critical Risk: 51+
/// </remarks>
private decimal CalculateTransactionRiskScore(Transaction transaction, Account account)
{
    // Amount factor calculation (30% weight)
    decimal amountScore = transaction.Amount.Amount switch
    {
        < 100m => 0m,
        <= 1000m => transaction.Amount.Amount / 100m,
        _ => 10m + (transaction.Amount.Amount - 1000m) / 1000m
    };

    // ... rest of implementation
}
```

## API Documentation

### Controller Documentation

```csharp
/// <summary>
/// API controller for managing bank accounts and account-related operations.
/// </summary>
/// <remarks>
/// This controller provides endpoints for:
/// <list type="bullet">
/// <item><description>Creating new customer accounts</description></item>
/// <item><description>Retrieving account information and balances</description></item>
/// <item><description>Updating account status and settings</description></item>
/// <item><description>Closing accounts</description></item>
/// </list>
///
/// <para>
/// All endpoints require authentication using JWT tokens. Some operations
/// require additional authorization based on user roles or account ownership.
/// </para>
///
/// <para>
/// Rate limiting is applied to prevent abuse:
/// - Account creation: 5 requests per hour per user
/// - Balance inquiries: 100 requests per hour per user
/// - Status updates: 10 requests per hour per user
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public class AccountController : ControllerBase
{
    /// <summary>
    /// Creates a new customer account with the specified details.
    /// </summary>
    /// <param name="request">The account creation request containing customer and account details</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>
    /// Returns the created account details including the generated account number.
    /// </returns>
    /// <response code="201">Account created successfully</response>
    /// <response code="400">Invalid request data or business rule violation</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="409">Account already exists for customer</response>
    /// <response code="429">Rate limit exceeded</response>
    /// <response code="500">Internal server error</response>
    /// <example>
    /// <code>
    /// POST /api/v1/accounts
    /// Content-Type: application/json
    /// Authorization: Bearer {jwt-token}
    ///
    /// {
    ///   "customerId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "accountType": "Checking",
    ///   "initialDeposit": 100.00,
    ///   "currency": "USD"
    /// }
    /// </code>
    /// </example>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AccountDto>> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

## README Documentation

### Service README Template

````markdown
# Account Service

## Overview

The Account Service is responsible for managing customer bank accounts within the Bank System Microservices architecture. It handles account creation, management, and provides account-related data to other services.

## Features

- **Account Management**: Create, update, and close customer accounts
- **Balance Tracking**: Real-time balance updates and history
- **Account Types**: Support for checking, savings, and money market accounts
- **Compliance**: Built-in regulatory compliance and audit trails
- **Security**: Role-based access control and data encryption

## Architecture

This service follows Clean Architecture principles with the following layers:

- **API Layer**: REST controllers and middleware
- **Application Layer**: Command/query handlers and business logic
- **Domain Layer**: Domain entities, value objects, and business rules
- **Infrastructure Layer**: Data access, external integrations, and messaging

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQL Server 2019 or Azure SQL Database
- Azure Service Bus (for event messaging)

### Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=AccountService;..."
  },
  "AzureServiceBus": {
    "ConnectionString": "Endpoint=...",
    "Topics": {
      "AccountEvents": "account-events"
    }
  }
}
```
````

### Running the Service

```bash
# Development
dotnet run --project src/BankSystem.Account.Api

# Production
dotnet publish -c Release
dotnet BankSystem.Account.Api.dll
```

## API Documentation

### Base URL

- Development: `https://localhost:5001/api/v1`
- Production: `https://api.bankystem.com/accounts/v1`

### Authentication

All endpoints require JWT authentication:

```
Authorization: Bearer {your-jwt-token}
```

### Core Endpoints

#### Create Account

```http
POST /accounts
Content-Type: application/json

{
  "customerId": "uuid",
  "accountType": "Checking|Savings|MoneyMarket",
  "initialDeposit": 100.00,
  "currency": "USD"
}
```

#### Get Account

```http
GET /accounts/{accountId}
```

#### Update Account Status

```http
PATCH /accounts/{accountId}/status
Content-Type: application/json

{
  "status": "Active|Suspended|Closed",
  "reason": "Optional reason for status change"
}
```

## Business Rules

### Account Creation

- Minimum initial deposit: $25.00
- Maximum accounts per customer: 10
- Supported currencies: USD, EUR, GBP
- Account numbers are auto-generated (10 digits)

### Account Limits

- Daily withdrawal limit: $5,000
- Monthly transfer limit: $50,000
- Overdraft protection: Optional, up to $1,000

## Error Handling

The service uses standardized error responses:

```json
{
  "type": "https://api.banksystem.com/errors/validation-error",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "errors": {
    "Amount": ["Amount must be greater than $25.00"]
  }
}
```

## Monitoring and Logging

### Health Checks

- Health endpoint: `/health`
- Detailed health: `/health/detailed`

### Metrics

- Custom metrics available at `/metrics`
- Application Insights integration

### Logging

- Structured logging using Serilog
- Log levels configurable via appsettings
- Sensitive data is automatically redacted

## Development

### Running Tests

```bash
# Unit tests
dotnet test tests/BankSystem.Account.Unit.Tests

# Integration tests
dotnet test tests/BankSystem.Account.Integration.Tests

# All tests
dotnet test
```

### Code Quality

- SonarQube analysis configured
- Code coverage minimum: 80%
- All public APIs must have XML documentation

## Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY . /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "BankSystem.Account.Api.dll"]
```

### Kubernetes

Helm charts available in `/deploy/helm/account-service`

## Contributing

Please see [CONTRIBUTING.md](../CONTRIBUTING.md) for development guidelines and coding standards.

## License

This project is licensed under the MIT License - see [LICENSE](../LICENSE) for details.

```

## Documentation Best Practices

### General Guidelines

1. **Write for your audience** - Consider who will read the documentation
2. **Keep it current** - Update documentation when code changes
3. **Use clear examples** - Provide realistic, working examples
4. **Be consistent** - Use the same style and format throughout
5. **Include error cases** - Document what can go wrong and why

### XML Documentation Guidelines

1. **Always document public APIs** - All public classes, methods, and properties
2. **Use proper XML tags** - `<summary>`, `<param>`, `<returns>`, `<exception>`
3. **Be specific with parameters** - Explain what values are expected
4. **Document exceptions** - Include when and why exceptions are thrown
5. **Add examples for complex APIs** - Use `<example>` and `<code>` tags

### Code Comment Guidelines

1. **Explain the WHY, not the WHAT** - Code should be self-explanatory for what it does
2. **Document business rules** - Explain regulatory requirements and business logic
3. **Mark technical debt** - Use TODO, HACK, or NOTE comments appropriately
4. **Keep comments close to code** - Place comments near the code they describe
5. **Remove obsolete comments** - Delete comments when code changes

### API Documentation

1. **Use OpenAPI/Scalar** - Generate interactive API documentation
2. **Include request/response examples** - Show realistic data
3. **Document error responses** - Include all possible HTTP status codes
4. **Explain authentication** - Document required headers and tokens
5. **Version your APIs** - Include version information in documentation

Remember: Good documentation is an investment in your future self and your team. Take time to write clear, helpful documentation that will save hours of confusion later.
```
