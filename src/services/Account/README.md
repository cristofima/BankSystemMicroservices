# Account Service

The Account Service manages customer accounts, balances, and account-related operations within the Bank System Microservices architecture. It handles account creation, balance management, and account status operations while maintaining data consistency through event-driven communication.

## 🎯 Service Overview

### Responsibilities

- **Account Management**: Create, update, and manage customer accounts
- **Balance Management**: Track and update account balances
- **Account Status**: Handle account activation, deactivation, and suspension
- **Account Validation**: Validate account operations and business rules
- **Event Handling**: Process transaction events to update balances

### Domain Boundaries

- Customer account information
- Account balances and financial state
- Account status and lifecycle
- Account-related business rules

## 🏗️ Architecture

### Clean Architecture Layers

```
Account.Api/               # Presentation Layer
├── Controllers/           # API Controllers
├── Middleware/           # Custom middleware
├── Extensions/           # Service extensions
└── Program.cs           # Application startup

Account.Application/       # Application Layer
├── Commands/            # CQRS Commands (CreateAccount, UpdateBalance)
├── Queries/            # CQRS Queries (GetAccount, GetAccountBalance)
├── Handlers/           # Command & Query Handlers
├── DTOs/              # Data Transfer Objects
├── Interfaces/        # Application Interfaces
├── Validators/        # FluentValidation Validators
└── Mappers/          # AutoMapper Profiles

Account.Domain/           # Domain Layer
├── Entities/            # Domain Entities (Account, Customer)
├── ValueObjects/       # Value Objects (AccountNumber, Money)
├── Events/            # Domain Events (AccountCreated, BalanceUpdated)
├── Enums/            # Domain Enumerations (AccountType, AccountStatus)
└── Exceptions/       # Domain Exceptions

Account.Infrastructure/   # Infrastructure Layer
├── Data/              # EF Core DbContext
├── Repositories/      # Repository Implementations
├── EventHandlers/     # Domain Event Handlers
└── Services/          # External Service Integrations
```

## 🔧 Features

### Account Management

- **Account Creation**: Create new customer accounts with validation
- **Account Updates**: Modify account information and settings
- **Account Closure**: Close accounts with proper validation
- **Account Types**: Support for different account types (Checking, Savings)

### Balance Management

- **Real-time Balances**: Maintain up-to-date account balances
- **Balance Validation**: Prevent overdrafts and invalid operations
- **Balance History**: Track balance changes over time
- **Multiple Currencies**: Support for different currency types

### Event Processing

- **Transaction Events**: Process events from Transaction Service
- **Balance Updates**: Update balances based on transaction events
- **Event Sourcing**: Maintain audit trail of all changes
- **Idempotency**: Handle duplicate events gracefully

## 🔌 API Endpoints

### Account Management Endpoints

#### POST /api/accounts

Create a new customer account.

**Request Body:**

```json
{
  "customerId": "guid",
  "accountType": "Checking",
  "initialDeposit": 100.0,
  "currency": "USD"
}
```

#### GET /api/accounts/{accountId}

Get account details by ID.

**Response:**

```json
{
  "id": "guid",
  "accountNumber": "1234567890",
  "customerId": "guid",
  "accountType": "Checking",
  "balance": 1500.0,
  "currency": "USD",
  "status": "Active",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

#### GET /api/accounts/customer/{customerId}

Get all accounts for a customer.

#### PUT /api/accounts/{accountId}/status

Update account status (activate, deactivate, suspend).

#### GET /api/accounts/{accountId}/balance

Get current account balance.

**Response:**

```json
{
  "accountId": "guid",
  "balance": 1500.0,
  "availableBalance": 1500.0,
  "currency": "USD",
  "lastUpdated": "2024-01-15T10:30:00Z"
}
```

## 🗄️ Data Model

### Account Entity

```csharp
public class Account : AggregateRoot<Guid>
{
    public string AccountNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    public AccountType AccountType { get; private set; }
    public Money Balance { get; private set; }
    public AccountStatus Status { get; private set; }

    // Domain methods
    public void Deposit(Money amount, string reference);
    public void Withdraw(Money amount, string reference);
    public void UpdateStatus(AccountStatus newStatus);
}
```

### Value Objects

```csharp
public record Money(decimal Amount, Currency Currency)
{
    public Money Add(Money other) => /* implementation */;
    public Money Subtract(Money other) => /* implementation */;
}

public record AccountNumber(string Value)
{
    // Validation logic
}
```

### Domain Events

```csharp
public record AccountCreatedEvent(
    Guid AccountId,
    string AccountNumber,
    Guid CustomerId,
    AccountType AccountType) : DomainEvent;

public record BalanceUpdatedEvent(
    Guid AccountId,
    Money PreviousBalance,
    Money NewBalance,
    string Reference) : DomainEvent;
```

## ⚙️ Configuration

### Database Schema

- **Accounts**: Main account information
- **AccountBalances**: Current and historical balances
- **AccountTransactions**: Account-level transaction history
- **Customers**: Customer information (if not managed by separate service)

### Event Subscriptions

- **TransactionCreatedEvent**: Update account balance
- **TransactionReversedEvent**: Reverse balance changes
- **AccountStatusChangedEvent**: Handle status changes

## 🧪 Testing Strategy

### Unit Tests

- Domain entity behavior
- Value object validation
- Command and query handlers
- Event handler logic

### Integration Tests

- API endpoint testing
- Database operations
- Event processing
- External service interactions

## 📊 Monitoring & Metrics

### Key Metrics

- Account creation rate
- Balance update frequency
- Account status changes
- Event processing latency
- Balance validation failures

### Health Checks

- Database connectivity
- Event subscription health
- Balance consistency checks
- External service availability

## 🚀 Deployment Notes

### Database Migrations

- Account schema setup
- Index optimization for queries
- Data seeding for initial accounts

### Azure Configuration

- Container Apps deployment
- Service Bus subscriptions
- Database connection strings
- Monitoring and logging setup

## 🔄 Event Flow

### Balance Update Flow

1. Transaction Service creates transaction
2. TransactionCreatedEvent published to Service Bus
3. Account Service receives event
4. Account balance updated
5. BalanceUpdatedEvent published
6. Movement Service receives event for history

## 📚 Implementation Status

🚧 **This service is planned for implementation**

Key components to implement:

- [ ] Domain entities and value objects
- [ ] CQRS commands and queries
- [ ] Event handlers for transaction events
- [ ] API controllers and validation
- [ ] Database context and repositories
- [ ] Unit and integration tests

## 🤝 Contributing

When implementing this service, ensure:

1. Follow Clean Architecture principles
2. Implement proper domain validation
3. Handle events idempotently
4. Maintain data consistency
5. Include comprehensive testing

## 📖 Related Documentation

- [Transaction Service](../Transaction/README.md) - For transaction event integration
- [Movement Service](../Movement/README.md) - For movement history
- [Security Service](../Security/README.md) - For authentication requirements
