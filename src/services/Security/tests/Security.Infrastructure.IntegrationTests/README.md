# Security.Infrastructure.IntegrationTests

This project contains integration tests for the Security.Infrastructure layer of the Bank System Microservices project.

## Overview

The integration tests validate the infrastructure components including:

- Database operations and entity relationships
- Data access patterns and performance
- Infrastructure service implementations
- Cross-cutting concerns like logging and configuration

## Test Structure

```
Security.Infrastructure.IntegrationTests/
├── Common/
│   └── SecurityTestBase.cs          # Base class for all integration tests
├── Simplified/
│   └── SimplifiedIntegrationTests.cs # Basic integration tests for core functionality
└── README.md                        # This file
```

## Current Implementation

### Integration Tests Status: ✅ WORKING

The integration tests are fully implemented and working with Testcontainers. All tests pass successfully:

- **Database Creation**: Verifies SQL Server container startup and table creation
- **Entity Operations**: CRUD operations for ApplicationUser and RefreshToken entities
- **Relationships**: Tests entity relationships and cascade deletes
- **Indexes**: Verifies database indexes are created correctly
- **Concurrency**: Tests concurrent database operations
- **Connection Pooling**: Tests multiple database connections

**Test Results**: 7/7 tests passing (✅)
**Test Duration**: ~4 minutes (includes SQL Server container startup)
**Container**: Uses Testcontainers.MsSql for real SQL Server testing

## Test Infrastructure

### SecurityTestBase

The base class provides:

- SQL Server container setup using Testcontainers
- Entity Framework Core configuration
- Service provider setup
- Database lifecycle management (create/cleanup)
- Faker for test data generation

### Dependencies

The project uses the following key packages:

- **NUnit**: Testing framework
- **FluentAssertions**: Assertion library
- **Testcontainers.MsSql**: SQL Server container for tests
- **Microsoft.EntityFrameworkCore**: Database access
- **Bogus**: Test data generation

## Running Tests

### Prerequisites

- Docker Desktop (for SQL Server container)
- .NET 9.0 SDK

### Command Line

```bash
# From the test project directory
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "TestMethodName"
```

### Visual Studio

1. Open the solution in Visual Studio
2. Build the solution
3. Open Test Explorer (Test → Test Explorer)
4. Run All Tests or select specific tests

## Test Configuration

### Connection String

Tests use SQL Server containers with the following default configuration:

- **Password**: `TestPassword123!`
- **Database**: Created dynamically per test run
- **Cleanup**: Automatic container cleanup after tests

### Logging

Tests include console logging for debugging:

- Entity Framework queries (sensitive data logging enabled)
- Test execution details
- Container lifecycle events

## Adding New Tests

### Basic Test Pattern

```csharp
[Test]
public async Task YourTest_ShouldDoSomething_WhenConditionMet()
{
    // Arrange
    var entity = new YourEntity { /* properties */ };

    // Act
    DbContext.YourEntities.Add(entity);
    await DbContext.SaveChangesAsync();

    // Assert
    var result = await DbContext.YourEntities.FirstOrDefaultAsync();
    result.Should().NotBeNull();
}
```

### Using the Base Class

1. Inherit from `SecurityTestBase`
2. Use the provided `DbContext` for database operations
3. Use `ServiceProvider` for dependency injection
4. Use `Faker` for generating test data

### Test Data Creation

```csharp
private async Task<ApplicationUser> CreateTestUserAsync()
{
    var user = new ApplicationUser
    {
        UserName = $"testuser_{Guid.NewGuid():N}@example.com",
        Email = $"testuser_{Guid.NewGuid():N}@example.com",
        EmailConfirmed = true,
        FirstName = "Test",
        LastName = "User"
    };

    DbContext.Users.Add(user);
    await DbContext.SaveChangesAsync();
    return user;
}
```

## Future Enhancements

When the application services are stabilized, the integration tests can be expanded to include:

1. **Service Layer Tests**: Testing IRefreshTokenService, ITokenService, etc.
2. **Authentication Flow Tests**: End-to-end authentication scenarios
3. **Security Tests**: Token validation, audit logging, etc.
4. **Performance Tests**: Load testing, bulk operations, etc.
5. **Error Handling Tests**: Testing exception scenarios and resilience

## Troubleshooting

### Docker Issues

If tests fail due to Docker/container issues:

1. Ensure Docker Desktop is running
2. Check available disk space
3. Restart Docker Desktop if needed

### Build Issues

If the project doesn't build:

1. Ensure all referenced projects build successfully
2. Check package versions for compatibility
3. Clean and rebuild the solution

### Test Failures

Common issues:

- **Connection timeouts**: Increase test timeout or check Docker resources
- **Port conflicts**: Restart Docker or use different ports
- **Data conflicts**: Tests should clean up after themselves

## Contributing

When adding new integration tests:

1. Follow the existing naming conventions
2. Include appropriate assertions using FluentAssertions
3. Clean up test data to avoid test interference
4. Add documentation for complex test scenarios
5. Consider performance impact of tests

---

For more information about the overall testing strategy, see the [Integration Testing Guidelines](../../../../docs/guidelines/integration-testing.md).
