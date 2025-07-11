# Account Service

## Overview

The Account Service is responsible for managing customer bank accounts within the Bank System Microservices architecture. It handles the complete lifecycle of bank accounts including creation, status management, balance tracking, and account-related operations while ensuring regulatory compliance and business rule enforcement.

## Core Responsibilities

### What the Account Service DOES:

- **Account Lifecycle Management**: Create, activate, suspend, and close customer accounts
- **Account Information Management**: Store and retrieve account details (account number, type, status, metadata)
- **Account Status Management**: Handle account status transitions (Active, Suspended, Frozen, Closed)
- **Account Type Management**: Support different account types (Checking, Savings, Business, etc.)
- **Customer Account Relationships**: Manage relationships between customers and their accounts
- **Account Validation**: Enforce business rules for account creation and modifications
- **Account History**: Maintain audit trails for account changes
- **Regulatory Compliance**: Ensure accounts comply with banking regulations
- **Account Limits**: Manage account-specific limits and restrictions

### What the Account Service DOES NOT DO:

- **Balance Calculations**: Does not calculate or store actual account balances (delegated to Movement Service)
- **Transaction Processing**: Does not process financial transactions (handled by Transaction Service)
- **Customer Management**: Does not manage customer personal information (handled by Customer Service)
- **Payment Processing**: Does not handle payment operations (handled by Payment Service)
- **Notifications**: Does not send notifications directly (uses Notification Service)
- **Reporting**: Does not generate reports (uses Reporting Service)
- **Authentication**: Does not handle user authentication (handled by Security Service)

## Service Communication

### Synchronous Communication (HTTP/REST):

- **Receives calls from**:

  - Transaction Service: Account validation for transactions
  - Movement Service: Account existence validation
  - Customer Service: Account creation requests
  - Reporting Service: Account data queries
  - API Gateway: Direct account operations

- **Makes calls to**:
  - Security Service: Token validation and authorization
  - Customer Service: Customer existence validation
  - Notification Service: Account status change notifications

### Asynchronous Communication (Events):

- **Publishes Events**:

  - `AccountCreatedEvent`: When a new account is created
  - `AccountStatusChangedEvent`: When account status changes
  - `AccountUpdatedEvent`: When account details are modified
  - `AccountClosedEvent`: When an account is closed

- **Subscribes to Events**:
  - `CustomerCreatedEvent`: To enable account creation for new customers
  - `CustomerStatusChangedEvent`: To update related accounts when customer status changes
  - `TransactionCompletedEvent`: To track account activity patterns
  - `MovementCreatedEvent`: For account activity monitoring

## Architecture

This service follows Clean Architecture principles with the following layers:

- **API Layer**: REST controllers and middleware
- **Application Layer**: Command/query handlers and business logic orchestration
- **Domain Layer**: Account entities, value objects, business rules, and domain events
- **Infrastructure Layer**: Data access, external integrations, and event publishing

## Features

- **Account Management**: Create, update, and manage customer accounts
- **Status Tracking**: Real-time account status management with business rule enforcement
- **Account Types**: Support for checking, savings, business, and specialized accounts
- **Compliance**: Built-in regulatory compliance and audit trails
- **Security**: Role-based access control and data encryption
- **Event-Driven**: Publishes domain events for system integration
- **Validation**: Comprehensive business rule validation
- **Audit Trail**: Complete history of account changes

## Business Rules

### Account Creation

- Customer must exist and be in good standing
- Minimum initial deposit requirements vary by account type:
  - Checking: $25.00
  - Savings: $100.00
  - Business: $500.00
- Maximum accounts per customer: 10
- Account numbers are auto-generated (10 digits) and unique
- Supported account types: Checking, Savings, Business, Joint

### Account Status Management

- **Active**: Account can receive all operations
- **Suspended**: Temporary restriction, limited operations allowed
- **Frozen**: All operations blocked except viewing
- **Closed**: No operations allowed, account is archived

### Account Limits

- Daily withdrawal limit: Configurable per account type
- Monthly transfer limit: Configurable per account type

### Compliance Requirements

- KYC (Know Your Customer) validation required
- AML (Anti-Money Laundering) monitoring integration
- Regulatory reporting for account activities
- Data retention policies for closed accounts

## API Documentation

### Base URL

- Development: `https://localhost:5002/api/v1`
- Production: `https://api.banksystem.com/accounts/v1`

### Authentication

All endpoints require JWT authentication with appropriate scopes:

```
Authorization: Bearer {your-jwt-token}
```

### Core Endpoints

#### Create Account

```http
POST /accounts
Content-Type: application/json
Authorization: Bearer {token}

{
  "customerId": "123e4567-e89b-12d3-a456-426614174000",
  "accountType": "Checking",
  "initialDeposit": 100.00,
  "currency": "USD"
}
```

#### Get Account

```http
GET /accounts/{accountId}
Authorization: Bearer {token}
```

#### Update Account Status

```http
PATCH /accounts/{accountId}/status
Content-Type: application/json
Authorization: Bearer {token}

{
  "status": "Suspended",
  "reason": "Suspicious activity detected",
  "effectiveDate": "2025-07-09T00:00:00Z"
}
```

#### Get Customer Accounts

```http
GET /accounts/customer/{customerId}
Authorization: Bearer {token}
```

#### Close Account

```http
DELETE /accounts/{accountId}
Content-Type: application/json
Authorization: Bearer {token}

{
  "reason": "Customer request",
  "transferToAccountId": "456e7890-e89b-12d3-a456-426614174000"
}
```

## Development

### Running Tests

```bash
# Unit tests
dotnet test tests/Account.Application.UnitTests

# Integration tests
dotnet test tests/Account.Infrastructure.IntegrationTests

# All tests
dotnet test
```

### Unit Testing Examples (xUnit)

```csharp
[Fact]
public async Task CreateAccount_WithValidData_ShouldReturnSuccess()
{
    // Arrange
    var command = new CreateAccountCommand
    {
        CustomerId = Guid.NewGuid(),
        AccountType = AccountType.Checking,
        InitialDeposit = 100m
    };

    var mockRepository = new Mock<IAccountRepository>();
    var mockCustomerService = new Mock<ICustomerService>();

    mockCustomerService.Setup(x => x.CustomerExistsAsync(command.CustomerId))
        .ReturnsAsync(true);

    mockRepository.Setup(x => x.GetAccountCountByCustomerAsync(command.CustomerId))
        .ReturnsAsync(2);

    var handler = new CreateAccountCommandHandler(
        mockRepository.Object,
        mockCustomerService.Object,
        Mock.Of<ILogger<CreateAccountCommandHandler>>());

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal(command.CustomerId, result.Value.CustomerId);
    mockRepository.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
}

[Fact]
public async Task CreateAccount_WithExistingCustomer_ShouldCreateAccount()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var command = new CreateAccountCommand
    {
        CustomerId = customerId,
        AccountType = AccountType.Savings,
        InitialDeposit = 500m
    };

    var mockRepository = new Mock<IAccountRepository>();
    var mockCustomerService = new Mock<ICustomerService>();

    mockCustomerService.Setup(x => x.CustomerExistsAsync(customerId))
        .ReturnsAsync(true);

    mockRepository.Setup(x => x.GetAccountCountByCustomerAsync(customerId))
        .ReturnsAsync(1);

    var handler = new CreateAccountCommandHandler(
        mockRepository.Object,
        mockCustomerService.Object,
        Mock.Of<ILogger<CreateAccountCommandHandler>>());

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(AccountType.Savings, result.Value.AccountType);
    Assert.Equal(500m, result.Value.InitialDeposit);
}

[Fact]
public async Task UpdateAccountStatus_WithValidData_ShouldUpdateStatus()
{
    // Arrange
    var accountId = Guid.NewGuid();
    var command = new UpdateAccountStatusCommand
    {
        AccountId = accountId,
        NewStatus = AccountStatus.Suspended,
        Reason = "Security review"
    };

    var existingAccount = Account.CreateNew(
        "1234567890",
        Guid.NewGuid(),
        AccountType.Checking,
        100m);

    var mockRepository = new Mock<IAccountRepository>();
    mockRepository.Setup(x => x.GetByIdAsync(accountId))
        .ReturnsAsync(existingAccount);

    var handler = new UpdateAccountStatusCommandHandler(
        mockRepository.Object,
        Mock.Of<ILogger<UpdateAccountStatusCommandHandler>>());

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(AccountStatus.Suspended, existingAccount.Status);
    mockRepository.Verify(x => x.UpdateAsync(existingAccount), Times.Once);
}

[Theory]
[InlineData(AccountType.Checking, 25.00)]
[InlineData(AccountType.Savings, 100.00)]
[InlineData(AccountType.Business, 500.00)]
public void Account_MinimumDeposit_ShouldBeValidForAccountType(
    AccountType accountType,
    decimal expectedMinimum)
{
    // Arrange & Act
    var minimumDeposit = Account.GetMinimumDeposit(accountType);

    // Assert
    Assert.Equal(expectedMinimum, minimumDeposit);
}
```

### Integration Testing Examples (xUnit)

```csharp
public class AccountControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AccountControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateAccount_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            CustomerId = Guid.NewGuid(),
            AccountType = AccountType.Checking,
            InitialDeposit = 100m,
            Currency = "USD"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AccountDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(result);
        Assert.Equal(request.CustomerId, result.CustomerId);
        Assert.Equal(request.AccountType.ToString(), result.AccountType);
    }

    [Fact]
    public async Task GetAccount_WithExistingId_ShouldReturnAccount()
    {
        // Arrange
        var accountId = await CreateTestAccountAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/accounts/{accountId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var account = JsonSerializer.Deserialize<AccountDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(account);
        Assert.Equal(accountId, account.Id);
    }

    [Fact]
    public async Task UpdateAccountStatus_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var accountId = await CreateTestAccountAsync();
        var updateRequest = new UpdateAccountStatusRequest
        {
            Status = AccountStatus.Suspended,
            Reason = "Integration test"
        };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/accounts/{accountId}/status",
            updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<Guid> CreateTestAccountAsync()
    {
        var request = new CreateAccountRequest
        {
            CustomerId = Guid.NewGuid(),
            AccountType = AccountType.Checking,
            InitialDeposit = 250m,
            Currency = "USD"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/accounts", request);
        var content = await response.Content.ReadAsStringAsync();
        var account = JsonSerializer.Deserialize<AccountDto>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return account.Id;
    }
}
```
