# GitHub Copilot Instructions for Bank System Microservices

## Project Overview

This is a .NET 9 microservices-based banking system built with Clean Architecture, SOLID principles, and Event-Driven Architecture (EDA) patterns. The system implements CQRS pattern and is designed for deployment on Azure.

## Architecture Patterns

- **Clean Architecture**: Each microservice follows Clean Architecture with clear separation of concerns
- **CQRS Pattern**: Commands and Queries are separated for better scalability and maintainability
- **Event-Driven Architecture**: Microservices communicate through Azure Service Bus events
- **Domain-Driven Design**: Each microservice represents a bounded context

## Project Structure Guidelines

### Microservice Structure

Each microservice follows this structure:

```
/ServiceName/
├── src/
│   ├── ServiceName.Api/          # Web API layer (Controllers, Program.cs)
│   ├── ServiceName.Application/  # Application layer (Commands, Queries, Handlers)
│   ├── ServiceName.Domain/       # Domain layer (Entities, Events, Value Objects)
│   └── ServiceName.Infrastructure/ # Infrastructure layer (Data, Messaging, External services)
└── tests/
    ├── ServiceName.Application.UnitTests/
    └── ServiceName.Api.IntegrationTests/
```

### Layer Responsibilities

- **API Layer**: Controllers, middleware, API configuration, dependency injection setup
- **Application Layer**: Commands, queries, handlers, DTOs, interfaces, validators
- **Domain Layer**: Entities, domain events, value objects, domain services
- **Infrastructure Layer**: Data access, external service integrations, messaging

## Coding Standards

### .NET Best Practices

- Use **record types** for DTOs and value objects
- Implement **nullable reference types** throughout the codebase
- Use **async/await** for all I/O operations
- Follow **C# naming conventions** (PascalCase for public members, camelCase for private)
- Use **dependency injection** for all dependencies

### SOLID Principles

- **Single Responsibility**: Each class should have one reason to change
- **Open/Closed**: Classes should be open for extension, closed for modification
- **Liskov Substitution**: Derived classes must be substitutable for base classes
- **Interface Segregation**: No client should depend on methods it doesn't use
- **Dependency Inversion**: Depend on abstractions, not concretions

### Clean Code Practices

- Write **self-documenting code** with meaningful names
- Keep methods **small and focused** (max 20 lines)
- Use **guard clauses** for early returns
- Avoid **deep nesting** (max 3 levels)
- **Extract complex logic** into separate methods or classes

### Domain-Driven Design

- Use **ubiquitous language** from business domain
- Create **rich domain models** with behavior, not just data
- Implement **domain events** for cross-boundary communication
- Use **value objects** for complex types without identity
- Define **aggregates** with clear boundaries and consistency rules

### Event-Driven Architecture

- Publish **domain events** after successful operations
- Use **idempotent event handlers** to handle duplicate events
- Implement **event versioning** for backward compatibility
- Design events as **immutable records**
- Include **correlation IDs** for tracing

## Azure Integration Guidelines

### Authentication & Security

- Use **Azure Managed Identity** for service-to-service authentication
- Store secrets in **Azure Key Vault**
- Implement **Azure AD authentication** for user access
- Use **HTTPS** for all communications
- Apply **principle of least privilege**

### Messaging & Events

- Use **Azure Service Bus** for reliable messaging
- Implement **retry policies** with exponential backoff
- Handle **dead letter queues** appropriately
- Use **topics and subscriptions** for event distribution
- Implement **message deduplication**

### Data Storage

- Use **Entity Framework Core** with appropriate database providers
- Implement **database migrations** for schema changes
- Use **connection pooling** for performance
- Implement **read replicas** for query optimization
- Apply **data encryption** at rest and in transit

### Monitoring & Observability

- Use **Azure Application Insights** for telemetry
- Implement **structured logging** with Serilog
- Add **health checks** for all dependencies
- Use **correlation IDs** for request tracing
- Monitor **key performance indicators** (KPIs)

## Code Generation Rules

### Command Pattern (CQRS)

```csharp
// Command example
public record CreateTransactionCommand(
    Guid AccountId,
    decimal Amount,
    TransactionType Type,
    string Description) : IRequest<TransactionDto>;

// Handler example
public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    // Implementation with validation, domain logic, and event publishing
}
```

### Domain Events

```csharp
// Domain event example
public record TransactionCreatedEvent(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    TransactionType Type,
    DateTime Timestamp) : IDomainEvent;
```

### API Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    // Use minimal API style with IMediator
    // Implement proper error handling
    // Return appropriate HTTP status codes
}
```

### Error Handling

- Use **global exception middleware** for unhandled exceptions
- Create **custom exceptions** for domain-specific errors
- Return **appropriate HTTP status codes**
- Include **correlation IDs** in error responses
- Log errors with **sufficient context**

### Validation

- Use **FluentValidation** for request validation
- Implement **domain validation** in entities
- Validate at **API boundary** and **domain boundary**
- Return **detailed validation errors** to clients

### Testing Guidelines

- Write **unit tests** for all business logic
- Create **integration tests** for API endpoints
- Use **test doubles** (mocks, stubs) appropriately
- Follow **AAA pattern** (Arrange, Act, Assert)
- Achieve **minimum 80% code coverage**

## Microservices Communication

### Synchronous Communication

- Use **HTTP REST APIs** for real-time queries
- Implement **circuit breaker** pattern for resilience
- Use **correlation IDs** for request tracing
- Apply **timeout policies** for external calls

### Asynchronous Communication

- Use **domain events** for eventual consistency
- Implement **event sourcing** where appropriate
- Handle **out-of-order events** gracefully
- Design for **idempotency**

## Performance Guidelines

- Use **async/await** for I/O operations
- Implement **caching** strategies with Redis
- Use **database indexing** effectively
- Apply **pagination** for large result sets
- Monitor **response times** and **throughput**

## Security Guidelines

- Validate **all inputs** at API boundary
- Use **parameterized queries** to prevent SQL injection
- Implement **rate limiting** to prevent abuse
- Apply **CORS policies** appropriately
- Log **security events** for auditing

Remember: Always prioritize code quality, maintainability, and security over quick solutions. Each microservice should be independently deployable and scalable.
