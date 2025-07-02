# Documentation Guidelines Overview

## Project Documentation Structure

This document provides an overview of the comprehensive documentation structure for the Bank System Microservices project, designed to enhance developer productivity and code quality through GitHub Copilot integration.

## Guidelines Organization

The documentation is organized into modular, focused guideline files that serve specific purposes:

### Code Generation Guidelines

- **[Code Generation](code-generation.md)** - Comprehensive .NET 9 code generation standards
- **[API Design](api-design.md)** - RESTful API design best practices
- **[Configuration Management](configuration-management.md)** - IOptions pattern and configuration best practices
- **[Entity Framework](entity-framework.md)** - Entity Framework Core migrations and best practices

### Code Review Guidelines

- **[Code Review](code-review.md)** - General code review practices and quality standards
- **[Security Review](security-review.md)** - Security-focused review guidelines following OWASP
- **[Performance Review](performance-review.md)** - Performance optimization and review guidelines

### Testing Guidelines

- **[Unit Testing](unit-testing.md)** - Comprehensive unit testing practices
- **[Integration Testing](integration-testing.md)** - End-to-end and integration testing strategies

## VS Code Integration

The guidelines are integrated with VS Code through `.vscode/settings.json` configuration:

```json
{
  "github.copilot.chat.codeGeneration.instructions": [
    { "file": "./docs/guidelines/code-generation.md" },
    { "file": "./docs/guidelines/api-design.md" },
    { "file": "./docs/guidelines/configuration-management.md" }
  ],
  "github.copilot.chat.codeReview.instructions": [
    { "file": "./docs/guidelines/code-review.md" },
    { "file": "./docs/guidelines/security-review.md" },
    { "file": "./docs/guidelines/performance-review.md" }
  ],
  "github.copilot.chat.unitTest.instructions": [
    { "file": "./docs/guidelines/unit-testing.md" },
    { "file": "./docs/guidelines/integration-testing.md" }
  ]
}
```

## Architecture Principles

### Clean Architecture

The project follows Clean Architecture principles with clear separation of concerns:

- **Domain Layer**: Entities, Value Objects, Domain Services, Domain Events
- **Application Layer**: Commands, Queries, Handlers, DTOs, Validators
- **Infrastructure Layer**: Data Access, External Services, Messaging
- **API Layer**: Controllers, Middleware, Configuration

### SOLID Principles

All code should adhere to SOLID principles:

- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes must be substitutable for base classes
- **Interface Segregation**: No client should depend on methods it doesn't use
- **Dependency Inversion**: Depend on abstractions, not concretions

### Event-Driven Architecture

Microservices communicate through domain events:

- **Domain Events**: Published after successful domain operations
- **Event Handlers**: Process events asynchronously
- **Azure Service Bus**: Reliable message delivery
- **Event Sourcing**: Where appropriate for audit trails

## Technology Stack

### Core Technologies

- **.NET 9**: Latest .NET framework
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: ORM for data access
- **MediatR**: CQRS pattern implementation
- **FluentValidation**: Input validation
- **AutoMapper**: Object mapping

### Azure Services

- **Azure Service Bus**: Message queuing
- **Azure Key Vault**: Secrets management
- **Azure Application Insights**: Monitoring and telemetry
- **Azure SQL Database**: Primary data store
- **Azure Cache for Redis**: Distributed caching

### Testing Frameworks

- **NUnit**: Unit testing framework
- **Moq**: Mocking framework
- **FluentAssertions**: Assertion library
- **Testcontainers**: Integration testing with containers
- **WireMock**: HTTP service mocking

## Code Quality Standards

### Naming Conventions

- **Classes**: PascalCase (e.g., `AccountService`, `TransactionHandler`)
- **Methods**: PascalCase (e.g., `ProcessTransactionAsync`)
- **Properties**: PascalCase (e.g., `AccountId`, `Balance`)
- **Fields**: camelCase with underscore prefix (e.g., `_repository`)
- **Constants**: PascalCase (e.g., `MaxTransactionAmount`)

### Best Practices

- **Async/Await**: Use for all I/O operations
- **ConfigureAwait(false)**: In library code to avoid deadlocks
- **CancellationToken**: Support request cancellation
- **Result Pattern**: For error handling instead of exceptions
- **Guard Clauses**: Early validation and returns
- **Dependency Injection**: Constructor injection for dependencies

### Security Guidelines

- **Input Validation**: Validate all inputs at API boundary
- **Authentication**: JWT tokens with proper validation
- **Authorization**: Role-based and resource-based access control
- **Data Protection**: Encrypt sensitive data at rest
- **HTTPS**: Enforce secure connections
- **OWASP Compliance**: Follow OWASP Top 10 guidelines

## Performance Guidelines

### Database Optimization

- **Indexing**: Strategic indexes for common queries
- **Pagination**: Always paginate large result sets
- **Projection**: Select only needed fields
- **AsNoTracking()**: For read-only queries
- **Connection Pooling**: Optimize database connections

#### Entity Framework Migrations

**Package Configuration**: The `Microsoft.EntityFrameworkCore.Design` package should be placed only in the **API/Startup project** to avoid conflicts.

**Migration Commands** (run from the service root directory):

```powershell
# Create a new migration
dotnet ef migrations add MigrationName --project Security.Infrastructure --startup-project Security.Api

# Apply migrations to database
dotnet ef database update --project Security.Infrastructure --startup-project Security.Api

# List all migrations
dotnet ef migrations list --project Security.Infrastructure --startup-project Security.Api

# Remove last migration (if not applied to database)
dotnet ef migrations remove --project Security.Infrastructure --startup-project Security.Api

# Generate SQL script for migrations
dotnet ef migrations script --project Security.Infrastructure --startup-project Security.Api

# Apply migrations for specific environment
dotnet ef database update --project Security.Infrastructure --startup-project Security.Api --environment Production
```

**Best Practices**:

- Always specify both `--project` (where migrations are stored) and `--startup-project` (where DbContext is configured)
- Use descriptive migration names (e.g., `AddUserRoles`, `UpdateTransactionSchema`)
- Review generated migration code before applying to database
- Keep migrations small and focused on single changes
- Test migrations in development environment first
- Generate SQL scripts for production deployments

### Caching Strategy

- **Memory Caching**: For frequently accessed data
- **Distributed Caching**: Redis for cross-service caching
- **Cache Invalidation**: Proper cache cleanup strategies
- **Cache Expiration**: Appropriate TTL values

### API Performance

- **Response Compression**: Gzip and Brotli compression
- **HTTP Client Pooling**: Reuse HTTP connections
- **Rate Limiting**: Protect against abuse
- **Content Negotiation**: Support multiple formats

## Testing Strategy

### Unit Testing

- **Test Coverage**: Minimum 80% code coverage
- **AAA Pattern**: Arrange, Act, Assert structure
- **Test Builders**: Use builder pattern for test data
- **Mocking**: Mock external dependencies
- **Fast Tests**: Each test should run in < 100ms

### Integration Testing

- **End-to-End**: Test complete user workflows
- **Test Containers**: Use Docker for consistent environments
- **Database Testing**: Test against real database
- **API Testing**: Validate HTTP endpoints
- **Performance Testing**: Load and stress testing

## Documentation Standards

### Code Documentation

- **XML Comments**: Document public APIs
- **README Files**: Project setup and usage instructions
- **Architecture Decisions**: Document major design decisions
- **API Documentation**: OpenAPI/Swagger specifications

### Process Documentation

- **Git Workflow**: Branch naming and commit message conventions
- **Code Review Process**: Review checklist and guidelines
- **Deployment Process**: CI/CD pipeline documentation
- **Troubleshooting**: Common issues and solutions

## Continuous Improvement

### Code Analysis

- **Static Analysis**: Use Roslyn analyzers
- **Code Metrics**: Monitor complexity and maintainability
- **Security Scanning**: Automated vulnerability detection
- **Performance Monitoring**: Track response times and resource usage

### Regular Reviews

- **Architecture Reviews**: Quarterly architecture assessments
- **Security Reviews**: Regular security audits
- **Performance Reviews**: Ongoing performance optimization
- **Documentation Updates**: Keep guidelines current

## Getting Started

1. **Read the Guidelines**: Familiarize yourself with all guideline documents
2. **Setup Development Environment**: Configure VS Code with recommended extensions
3. **Review Existing Code**: Study the current codebase structure
4. **Start Small**: Begin with simple changes following the guidelines
5. **Get Feedback**: Participate in code reviews and seek mentorship

## Resources

### Internal Documentation

- [Original Development Guidelines](../dotnet-development-guidelines.md)
- [Project README](../../README.md)
- [Git Commit Message Guidelines](../../.github/git-commit-messages-instructions.md)

### External Resources

- [Microsoft .NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Azure Documentation](https://docs.microsoft.com/en-us/azure/)
- [OWASP Guidelines](https://owasp.org/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

_This documentation is living and should be updated as the project evolves and new best practices emerge._
