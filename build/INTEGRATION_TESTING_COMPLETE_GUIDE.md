# Integration Testing Complete Guide

## Overview

This guide provides comprehensive instructions for implementing and running integration tests in the Bank System Microservices project, including Azure DevOps CI/CD configuration.

## Project Structure

### Security Service Integration Tests

```
Security.Infrastructure.IntegrationTests/
├── Common/
│   └── SecurityTestBase.cs          # Base test infrastructure
├── Simplified/
│   └── SimplifiedIntegrationTests.cs # Core integration tests
└── Security.Infrastructure.IntegrationTests.csproj
```

## Test Infrastructure

### SecurityTestBase Configuration

The base class provides:

- **SQL Server Container**: Using Testcontainers.MsSql
- **Entity Framework**: Real database operations
- **Service Provider**: Dependency injection setup
- **Test Data**: Faker/Bogus for realistic data generation
- **Lifecycle Management**: Container creation/cleanup

```csharp
public abstract class SecurityTestBase
{
    protected SecurityDbContext DbContext { get; private set; }
    protected IServiceProvider ServiceProvider { get; private set; }
    protected Faker Faker { get; private set; }
    private MsSqlContainer _mssqlContainer;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _mssqlContainer = new MsSqlBuilder()
            .WithPassword("TestPassword123!")
            .Build();
        await _mssqlContainer.StartAsync();

        // Configure services and DbContext
        SetupServices();
        SetupDatabase();
    }
}
```

## Test Categories

### 1. Database Operations

- Entity CRUD operations
- Relationship validation
- Cascade delete behavior
- Index verification

### 2. Concurrency Testing

- Thread-safe database access
- Connection pooling validation
- Concurrent user operations

### 3. Infrastructure Validation

- Service registration verification
- Configuration validation
- Database schema creation

## Azure DevOps CI/CD Integration

### Pipeline Configuration for Integration Tests

```yaml
# Use Ubuntu for better Docker performance
pool:
  vmImage: "ubuntu-latest"

steps:
  # Integration tests require Docker for SQL Server containers
  - task: DotNetCoreCLI@2
    displayName: "Run Integration Tests"
    inputs:
      command: "test"
      projects: "src/services/Security/tests/Security.Infrastructure.IntegrationTests/Security.Infrastructure.IntegrationTests.csproj"
      arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage" --settings "$(Build.SourcesDirectory)/src/coverlet.runsettings"'
      publishTestResults: true
    # Allow failures for integration tests (Docker dependency)
    continueOnError: true
```

### Environment Variables for CI

Configure these for optimal Testcontainers behavior:

```yaml
variables:
  TESTCONTAINERS_RYUK_DISABLED: "true"
  TESTCONTAINERS_HOST_OVERRIDE: "localhost"
  TESTCONTAINERS_CHECKS_DISABLE: "true"
```

## Local Development Setup

### Prerequisites

- **Docker Desktop**: Required for SQL Server containers
- **.NET 9.0 SDK**: For building and running tests
- **Visual Studio/VS Code**: For development

### Running Tests Locally

```bash
# From project root
cd src/services/Security/tests/Security.Infrastructure.IntegrationTests

# Run all integration tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "TestMethodName"
```

## Test Implementation Examples

### Basic Integration Test Pattern

```csharp
[Test]
public async Task CanCreateAndRetrieveUser_WhenValidData_ShouldSucceed()
{
    // Arrange
    var user = new ApplicationUser
    {
        UserName = Faker.Internet.Email(),
        Email = Faker.Internet.Email(),
        FirstName = Faker.Name.FirstName(),
        LastName = Faker.Name.LastName()
    };

    // Act
    DbContext.Users.Add(user);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrievedUser = await DbContext.Users
        .FirstOrDefaultAsync(u => u.Email == user.Email);

    retrievedUser.Should().NotBeNull();
    retrievedUser.UserName.Should().Be(user.UserName);
}
```

### Concurrency Test Example

```csharp
[Test]
public async Task ConcurrentUserCreation_ShouldHandleMultipleUsers()
{
    // Arrange
    var tasks = new List<Task>();
    var userEmails = new List<string>();

    // Act - Create 10 users concurrently
    for (int i = 0; i < 10; i++)
    {
        var email = Faker.Internet.Email();
        userEmails.Add(email);

        tasks.Add(Task.Run(async () =>
        {
            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

            var user = new ApplicationUser { Email = email, UserName = email };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }));
    }

    await Task.WhenAll(tasks);

    // Assert
    var createdUsers = await DbContext.Users
        .Where(u => userEmails.Contains(u.Email))
        .ToListAsync();

    createdUsers.Should().HaveCount(10);
}
```

## Performance Considerations

### Container Optimization

- **Container Reuse**: Use OneTimeSetUp for container lifecycle
- **Resource Management**: Proper cleanup in OneTimeTearDown
- **Timeout Configuration**: Adequate timeouts for container startup

### Test Execution

- **Parallel Execution**: Configure for optimal performance
- **Database Cleanup**: Reset between test methods
- **Connection Pooling**: Validate proper connection management

## Troubleshooting

### Common Issues

#### Container Startup Failures

```bash
# Check Docker is running
docker ps

# Check container logs
docker logs <container_id>

# Verify ports are available
netstat -tulpn | grep 1433
```

#### Test Execution Issues

```bash
# Build before testing
dotnet build

# Check test discovery
dotnet test --list-tests

# Run with detailed logging
dotnet test --logger "console;verbosity=diagnostic"
```

### CI/CD Specific Issues

#### Docker Not Available

- Ensure using Ubuntu agents (better Docker support)
- Verify Docker service is running on self-hosted agents
- Check agent capabilities include Docker

#### Timeout Issues

- Increase test timeouts for container startup
- Configure appropriate pipeline timeouts
- Use `continueOnError: true` for integration tests

## Best Practices

### Test Design

- ✅ Use real SQL Server containers (not in-memory databases)
- ✅ Test actual business scenarios
- ✅ Validate database constraints and relationships
- ✅ Include performance/concurrency testing
- ✅ Proper test isolation and cleanup

### CI/CD Integration

- ✅ Separate unit and integration test execution
- ✅ Use Ubuntu agents for Docker-based tests
- ✅ Configure appropriate timeouts
- ✅ Allow integration test failures (infrastructure dependencies)
- ✅ Comprehensive logging for troubleshooting

### Infrastructure

- ✅ Testcontainers for consistent environments
- ✅ Service provider setup for dependency injection
- ✅ Realistic test data generation
- ✅ Proper resource cleanup

## Dependencies

```xml
<!-- Key NuGet packages for integration testing -->
<PackageReference Include="NUnit" Version="4.0.1" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Testcontainers.MsSql" Version="3.7.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
<PackageReference Include="Bogus" Version="35.4.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
```

## Results and Verification

### Expected Test Results

```
Test Run Summary:
- Total Tests: 7+ (growing with new features)
- Passed: All tests should pass
- Duration: ~2-4 minutes (includes container startup)
- Coverage: Focuses on infrastructure and data access layers
```

### Verification Steps

1. **Local Development**: All tests pass with Docker Desktop
2. **CI Pipeline**: Tests execute successfully on Ubuntu agents
3. **Code Coverage**: Integration test coverage reported separately
4. **Performance**: Container startup and test execution within acceptable limits

This guide consolidates all integration testing knowledge and provides complete setup instructions for both local development and CI/CD environments.
