# Integration Testing Guidelines

## Overview

This document provides comprehensive guidelines for writing effective integration tests in the Bank System Microservices project. Integration tests verify that different components work correctly together, including API endpoints, database operations, external services, and message handling.

## Integration Testing Framework Setup

### Test Project Configuration

```xml
<!-- Integration test project file (.csproj) -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="4.0.1" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
    <PackageReference Include="Testcontainers" Version="3.6.0" />
    <PackageReference Include="Testcontainers.SqlServer" Version="3.6.0" />
    <PackageReference Include="Testcontainers.Redis" Version="3.6.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Bogus" Version="35.4.0" />
    <PackageReference Include="WireMock.Net" Version="1.5.46" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\BankSystem.Account.Api\BankSystem.Account.Api.csproj" />
    <ProjectReference Include="..\..\src\BankSystem.Account.Infrastructure\BankSystem.Account.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

### Custom Web Application Factory

```csharp
// ✅ Good: Custom WebApplicationFactory for integration tests
public class BankSystemWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqlServerContainer _sqlServerContainer;
    private readonly RedisContainer _redisContainer;

    public BankSystemWebApplicationFactory()
    {
        _sqlServerContainer = new SqlServerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test@123456")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BankDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add test database
            services.AddDbContext<BankDbContext>(options =>
            {
                options.UseSqlServer(_sqlServerContainer.GetConnectionString());
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Replace Redis cache with test container
            var redisCacheDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDistributedCache));
            if (redisCacheDescriptor != null)
            {
                services.Remove(redisCacheDescriptor);
            }

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = _redisContainer.GetConnectionString();
            });

            // Replace external services with test doubles
            services.RemoveService<IExternalBankService>();
            services.AddScoped<IExternalBankService, TestExternalBankService>();

            services.RemoveService<IEmailService>();
            services.AddScoped<IEmailService, TestEmailService>();
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _sqlServerContainer.StartAsync();
        await _redisContainer.StartAsync();

        // Run database migrations
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BankDbContext>();
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlServerContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}

// Extension method for service removal
public static class ServiceCollectionExtensions
{
    public static IServiceCollection RemoveService<T>(this IServiceCollection services)
    {
        var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (serviceDescriptor != null)
        {
            services.Remove(serviceDescriptor);
        }
        return services;
    }
}
```

### Base Integration Test Class

```csharp
// ✅ Good: Base class for integration tests
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected BankSystemWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected BankDbContext DbContext { get; private set; } = null!;
    protected Faker Faker { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new BankSystemWebApplicationFactory();
        await Factory.InitializeAsync();

        Client = Factory.CreateClient();
        DbContext = Factory.Services.CreateScope().ServiceProvider.GetRequiredService<BankDbContext>();
        Faker = new Faker();
    }

    public async Task DisposeAsync()
    {
        DbContext?.Dispose();
        Client?.Dispose();
        await Factory.DisposeAsync();
    }

    protected async Task<Customer> CreateTestCustomerAsync()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName(),
            Email = Faker.Internet.Email(),
            PhoneNumber = Faker.Phone.PhoneNumber(),
            DateOfBirth = Faker.Date.PastDateOnly(years: 30, refDate: DateOnly.FromDateTime(DateTime.Now.AddYears(-18))),
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        await DbContext.SaveChangesAsync();

        return customer;
    }

    protected async Task<Account> CreateTestAccountAsync(Guid? customerId = null, decimal balance = 1000m)
    {
        var customer = customerId.HasValue
            ? await DbContext.Customers.FindAsync(customerId.Value)
            : await CreateTestCustomerAsync();

        var account = Account.CreateNew(
            Faker.Finance.Account(),
            customer!.Id,
            new Money(balance, Currency.USD));

        DbContext.Accounts.Add(account);
        await DbContext.SaveChangesAsync();

        return account;
    }

    protected async Task<ApplicationUser> CreateTestUserAsync(string email = null!)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email ?? Faker.Internet.Email(),
            Email = email ?? Faker.Internet.Email(),
            EmailConfirmed = true,
            PhoneNumber = Faker.Phone.PhoneNumber(),
            PhoneNumberConfirmed = true
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        return user;
    }

    protected async Task AuthenticateAsync(string email = "test@example.com")
    {
        var user = await CreateTestUserAsync(email);

        var token = GenerateJwtToken(user);
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-for-integration-tests-only-32-chars-long"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.UserName!)
        };

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

## API Integration Testing

### Testing Controller Endpoints

```csharp
// ✅ Good: Comprehensive API integration tests
[TestFixture]
public class AccountControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetAccount_ExistingAccount_ShouldReturnAccountDto()
    {
        // Arrange
        await AuthenticateAsync();
        var account = await CreateTestAccountAsync();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{account.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var accountDto = JsonSerializer.Deserialize<AccountDto>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        accountDto.Should().NotBeNull();
        accountDto!.Id.Should().Be(account.Id);
        accountDto.AccountNumber.Should().Be(account.AccountNumber);
        accountDto.Balance.Should().Be(account.Balance.Amount);
    }

    [Test]
    public async Task GetAccount_NonExistentAccount_ShouldReturnNotFound()
    {
        // Arrange
        await AuthenticateAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetAccount_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var account = await CreateTestAccountAsync();
        // No authentication

        // Act
        var response = await Client.GetAsync($"/api/accounts/{account.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateAccount_ValidRequest_ShouldCreateAccountAndReturnCreated()
    {
        // Arrange
        await AuthenticateAsync();
        var customer = await CreateTestCustomerAsync();

        var request = new CreateAccountRequest
        {
            CustomerId = customer.Id,
            AccountType = AccountType.Checking,
            InitialDeposit = 1500m,
            Currency = "USD"
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/accounts", requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdAccount = JsonSerializer.Deserialize<AccountDto>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        createdAccount.Should().NotBeNull();
        createdAccount!.Balance.Should().Be(request.InitialDeposit);
        createdAccount.Currency.Should().Be(request.Currency);

        // Verify in database
        var accountInDb = await DbContext.Accounts.FindAsync(createdAccount.Id);
        accountInDb.Should().NotBeNull();
        accountInDb!.CustomerId.Should().Be(customer.Id);
        accountInDb.Balance.Amount.Should().Be(request.InitialDeposit);

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/accounts/{createdAccount.Id}");
    }

    [Test]
    public async Task CreateAccount_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        await AuthenticateAsync();

        var request = new CreateAccountRequest
        {
            CustomerId = Guid.Empty, // Invalid
            AccountType = AccountType.Checking,
            InitialDeposit = -100m, // Invalid
            Currency = "INVALID" // Invalid
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/accounts", requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKeys("CustomerId", "InitialDeposit", "Currency");
    }

    [Test]
    public async Task GetAccountTransactions_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await AuthenticateAsync();
        var account = await CreateTestAccountAsync();

        // Create test transactions
        var transactions = new List<Transaction>();
        for (int i = 0; i < 15; i++)
        {
            var transaction = Transaction.CreateDeposit(
                account.Id,
                new Money(100m + i, Currency.USD),
                $"Test transaction {i}");
            transactions.Add(transaction);
        }

        DbContext.Transactions.AddRange(transactions);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{account.Id}/transactions?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var pagedResult = JsonSerializer.Deserialize<PagedResult<TransactionDto>>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        pagedResult.Should().NotBeNull();
        pagedResult!.Data.Should().HaveCount(10);
        pagedResult.TotalCount.Should().Be(15);
        pagedResult.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(10);
        pagedResult.TotalPages.Should().Be(2);
    }
}
```

### Testing with Different Content Types

```csharp
// ✅ Good: Testing different content types and formats
[TestFixture]
public class AccountStatementIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetAccountStatement_JsonFormat_ShouldReturnJsonData()
    {
        // Arrange
        await AuthenticateAsync();
        var account = await CreateTestAccountAsync();

        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await Client.GetAsync($"/api/accounts/{account.Id}/statement?year=2024&month=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var statement = JsonSerializer.Deserialize<AccountStatementDto>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        statement.Should().NotBeNull();
        statement!.AccountId.Should().Be(account.Id);
    }

    [Test]
    public async Task GetAccountStatement_PdfFormat_ShouldReturnPdfFile()
    {
        // Arrange
        await AuthenticateAsync();
        var account = await CreateTestAccountAsync();

        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/pdf"));

        // Act
        var response = await Client.GetAsync($"/api/accounts/{account.Id}/statement?year=2024&month=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var content = await response.Content.ReadAsByteArrayAsync();
        content.Should().NotBeEmpty();

        // Verify PDF header
        var pdfHeader = Encoding.ASCII.GetString(content.Take(4).ToArray());
        pdfHeader.Should().Be("%PDF");
    }
}
```

## Database Integration Testing

### Testing Repository Implementations

```csharp
// ✅ Good: Repository integration tests
[TestFixture]
public class AccountRepositoryIntegrationTests : IntegrationTestBase
{
    private IAccountRepository _repository;

    [SetUp]
    public async Task SetUp()
    {
        await InitializeAsync();
        _repository = Factory.Services.CreateScope().ServiceProvider.GetRequiredService<IAccountRepository>();
    }

    [Test]
    public async Task AddAsync_NewAccount_ShouldPersistToDatabase()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        var account = Account.CreateNew(
            Faker.Finance.Account(),
            customer.Id,
            new Money(1000m, Currency.USD));

        // Act
        await _repository.AddAsync(account);

        // Assert
        var savedAccount = await DbContext.Accounts.FindAsync(account.Id);
        savedAccount.Should().NotBeNull();
        savedAccount!.AccountNumber.Should().Be(account.AccountNumber);
        savedAccount.CustomerId.Should().Be(customer.Id);
        savedAccount.Balance.Amount.Should().Be(1000m);
    }

    [Test]
    public async Task GetByIdAsync_ExistingAccount_ShouldReturnAccountWithTransactions()
    {
        // Arrange
        var account = await CreateTestAccountAsync();

        // Add some transactions
        var transaction1 = Transaction.CreateDeposit(account.Id, new Money(500m, Currency.USD), "Deposit 1");
        var transaction2 = Transaction.CreateWithdrawal(account.Id, new Money(200m, Currency.USD), "Withdrawal 1");

        DbContext.Transactions.AddRange(transaction1, transaction2);
        await DbContext.SaveChangesAsync();

        // Act
        var retrievedAccount = await _repository.GetByIdAsync(account.Id);

        // Assert
        retrievedAccount.Should().NotBeNull();
        retrievedAccount!.Id.Should().Be(account.Id);
        retrievedAccount.Transactions.Should().HaveCount(2);
        retrievedAccount.Transactions.Should().Contain(t => t.Amount.Amount == 500m);
        retrievedAccount.Transactions.Should().Contain(t => t.Amount.Amount == 200m);
    }

    [Test]
    public async Task GetByAccountNumberAsync_ExistingAccount_ShouldReturnAccount()
    {
        // Arrange
        var account = await CreateTestAccountAsync();

        // Act
        var retrievedAccount = await _repository.GetByAccountNumberAsync(account.AccountNumber);

        // Assert
        retrievedAccount.Should().NotBeNull();
        retrievedAccount!.Id.Should().Be(account.Id);
        retrievedAccount.AccountNumber.Should().Be(account.AccountNumber);
    }

    [Test]
    public async Task GetByCustomerIdAsync_MultipleAccounts_ShouldReturnAllCustomerAccounts()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        var account1 = await CreateTestAccountAsync(customer.Id, 1000m);
        var account2 = await CreateTestAccountAsync(customer.Id, 2000m);
        var account3 = await CreateTestAccountAsync(); // Different customer

        // Act
        var customerAccounts = await _repository.GetByCustomerIdAsync(customer.Id);

        // Assert
        customerAccounts.Should().HaveCount(2);
        customerAccounts.Should().Contain(a => a.Id == account1.Id);
        customerAccounts.Should().Contain(a => a.Id == account2.Id);
        customerAccounts.Should().NotContain(a => a.Id == account3.Id);
    }

    [Test]
    public async Task UpdateAsync_ModifiedAccount_ShouldPersistChanges()
    {
        // Arrange
        var account = await CreateTestAccountAsync(balance: 1000m);

        // Act
        account.Deposit(new Money(500m, Currency.USD), "Test deposit");
        await _repository.UpdateAsync(account);

        // Assert
        var updatedAccount = await DbContext.Accounts.FindAsync(account.Id);
        updatedAccount.Should().NotBeNull();
        updatedAccount!.Balance.Amount.Should().Be(1500m);

        // Verify transaction was added
        var transactions = await DbContext.Transactions
            .Where(t => t.AccountId == account.Id)
            .ToListAsync();
        transactions.Should().ContainSingle();
        transactions.First().Amount.Amount.Should().Be(500m);
    }

    [Test]
    public async Task DeleteAsync_ExistingAccount_ShouldRemoveFromDatabase()
    {
        // Arrange
        var account = await CreateTestAccountAsync();

        // Act
        await _repository.DeleteAsync(account);

        // Assert
        var deletedAccount = await DbContext.Accounts.FindAsync(account.Id);
        deletedAccount.Should().BeNull();
    }
}
```

### Testing Database Transactions

```csharp
// ✅ Good: Testing transaction behavior
[TestFixture]
public class TransactionIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task TransferMoney_BothAccountsUpdated_ShouldCommitAtomically()
    {
        // Arrange
        var sourceAccount = await CreateTestAccountAsync(balance: 1000m);
        var targetAccount = await CreateTestAccountAsync(balance: 500m);

        var transferService = Factory.Services.CreateScope().ServiceProvider
            .GetRequiredService<ITransferService>();

        // Act
        var result = await transferService.TransferAsync(
            sourceAccount.Id,
            targetAccount.Id,
            new Money(300m, Currency.USD),
            "Integration test transfer");

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify both accounts were updated
        var updatedSourceAccount = await DbContext.Accounts.FindAsync(sourceAccount.Id);
        var updatedTargetAccount = await DbContext.Accounts.FindAsync(targetAccount.Id);

        updatedSourceAccount!.Balance.Amount.Should().Be(700m);
        updatedTargetAccount!.Balance.Amount.Should().Be(800m);

        // Verify transactions were created
        var sourceTransaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.AccountId == sourceAccount.Id && t.Type == TransactionType.Transfer);
        var targetTransaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.AccountId == targetAccount.Id && t.Type == TransactionType.Transfer);

        sourceTransaction.Should().NotBeNull();
        targetTransaction.Should().NotBeNull();
        sourceTransaction!.Amount.Amount.Should().Be(300m);
        targetTransaction!.Amount.Amount.Should().Be(300m);
    }

    [Test]
    public async Task TransferMoney_InsufficientFunds_ShouldRollbackChanges()
    {
        // Arrange
        var sourceAccount = await CreateTestAccountAsync(balance: 100m);
        var targetAccount = await CreateTestAccountAsync(balance: 500m);

        var transferService = Factory.Services.CreateScope().ServiceProvider
            .GetRequiredService<ITransferService>();

        // Act
        var result = await transferService.TransferAsync(
            sourceAccount.Id,
            targetAccount.Id,
            new Money(300m, Currency.USD), // More than available
            "Failed transfer test");

        // Assert
        result.IsFailure.Should().BeTrue();

        // Verify no changes were made
        var unchangedSourceAccount = await DbContext.Accounts.FindAsync(sourceAccount.Id);
        var unchangedTargetAccount = await DbContext.Accounts.FindAsync(targetAccount.Id);

        unchangedSourceAccount!.Balance.Amount.Should().Be(100m);
        unchangedTargetAccount!.Balance.Amount.Should().Be(500m);

        // Verify no transactions were created
        var transactions = await DbContext.Transactions
            .Where(t => t.AccountId == sourceAccount.Id || t.AccountId == targetAccount.Id)
            .ToListAsync();

        transactions.Should().BeEmpty();
    }
}
```

## External Service Integration Testing

### Using WireMock for External API Testing

```csharp
// ✅ Good: Testing external service integration with WireMock
[TestFixture]
public class ExternalBankServiceIntegrationTests : IntegrationTestBase
{
    private WireMockServer _wireMockServer;

    [SetUp]
    public async Task SetUp()
    {
        await InitializeAsync();

        _wireMockServer = WireMockServer.Start();

        // Configure the test to use our mock server
        var configuration = Factory.Services.GetRequiredService<IConfiguration>();
        configuration["ExternalServices:BankApi:BaseUrl"] = _wireMockServer.Url;
    }

    [TearDown]
    public void TearDown()
    {
        _wireMockServer?.Stop();
        _wireMockServer?.Dispose();
    }

    [Test]
    public async Task ValidateAccount_ExternalServiceReturnsValid_ShouldReturnSuccess()
    {
        // Arrange
        var accountNumber = "1234567890";
        var routingNumber = "987654321";

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/api/validate-account")
                .WithMethod("POST")
                .WithBodyAsJson(new { AccountNumber = accountNumber, RoutingNumber = routingNumber }))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { IsValid = true, BankName = "Test Bank" }));

        var externalBankService = Factory.Services.CreateScope().ServiceProvider
            .GetRequiredService<IExternalBankService>();

        // Act
        var result = await externalBankService.ValidateAccountAsync(accountNumber, routingNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.BankName.Should().Be("Test Bank");

        // Verify the request was made
        var requests = _wireMockServer.LogEntries.Select(e => e.RequestMessage).ToList();
        requests.Should().ContainSingle(r => r.Path == "/api/validate-account");
    }

    [Test]
    public async Task ValidateAccount_ExternalServiceFails_ShouldReturnFailure()
    {
        // Arrange
        var accountNumber = "1234567890";
        var routingNumber = "987654321";

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/api/validate-account")
                .WithMethod("POST"))
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal server error"));

        var externalBankService = Factory.Services.CreateScope().ServiceProvider
            .GetRequiredService<IExternalBankService>();

        // Act
        var result = await externalBankService.ValidateAccountAsync(accountNumber, routingNumber);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("External service error");
    }

    [Test]
    public async Task ValidateAccount_ExternalServiceTimeout_ShouldReturnFailureAfterRetries()
    {
        // Arrange
        var accountNumber = "1234567890";
        var routingNumber = "987654321";

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/api/validate-account")
                .WithMethod("POST"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(10)) // Simulate timeout
                .WithBodyAsJson(new { IsValid = true }));

        var externalBankService = Factory.Services.CreateScope().ServiceProvider
            .GetRequiredService<IExternalBankService>();

        // Act & Assert
        var stopwatch = Stopwatch.StartNew();
        var result = await externalBankService.ValidateAccountAsync(accountNumber, routingNumber);
        stopwatch.Stop();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timeout");

        // Should have failed before the full 10 second delay due to timeout and retries
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(8));
    }
}
```

### Testing Service Bus Integration

```csharp
// ✅ Good: Testing message publishing and handling
[TestFixture]
public class EventPublishingIntegrationTests : IntegrationTestBase
{
    private readonly List<IDomainEvent> _publishedEvents = new();

    [SetUp]
    public async Task SetUp()
    {
        await InitializeAsync();

        // Replace event publisher with test implementation
        var eventPublisherDescriptor = Factory.Services.SingleOrDefault(
            d => d.ServiceType == typeof(IEventPublisher));
        if (eventPublisherDescriptor != null)
        {
            Factory.Services.Remove(eventPublisherDescriptor);
        }
        Factory.Services.AddScoped<IEventPublisher>(_ => new TestEventPublisher(_publishedEvents));
    }

    [Test]
    public async Task CreateAccount_ShouldPublishAccountCreatedEvent()
    {
        // Arrange
        await AuthenticateAsync();
        var customer = await CreateTestCustomerAsync();

        var request = new CreateAccountRequest
        {
            CustomerId = customer.Id,
            AccountType = AccountType.Checking,
            InitialDeposit = 1000m,
            Currency = "USD"
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/accounts", requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        _publishedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AccountCreatedEvent>()
            .Which.CustomerId.Should().Be(customer.Id);
    }

    [Test]
    public async Task ProcessTransfer_ShouldPublishTransferEvents()
    {
        // Arrange
        await AuthenticateAsync();
        var sourceAccount = await CreateTestAccountAsync(balance: 1000m);
        var targetAccount = await CreateTestAccountAsync(balance: 500m);

        var request = new CreateTransferRequest
        {
            FromAccountId = sourceAccount.Id,
            ToAccountId = targetAccount.Id,
            Amount = 300m,
            Description = "Test transfer"
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/transfers", requestContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        _publishedEvents.Should().HaveCount(2);
        _publishedEvents.Should().Contain(e => e is MoneyWithdrawnEvent);
        _publishedEvents.Should().Contain(e => e is MoneyDepositedEvent);
    }
}

public class TestEventPublisher : IEventPublisher
{
    private readonly List<IDomainEvent> _events;

    public TestEventPublisher(List<IDomainEvent> events)
    {
        _events = events;
    }

    public Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        _events.Add(domainEvent);
        return Task.CompletedTask;
    }
}
```

## Performance Integration Testing

### Load Testing

```csharp
// ✅ Good: Performance integration tests
[TestFixture]
public class PerformanceIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetAccount_Under100ConcurrentRequests_ShouldMaintainPerformance()
    {
        // Arrange
        await AuthenticateAsync();
        var accounts = new List<Account>();
        for (int i = 0; i < 10; i++)
        {
            accounts.Add(await CreateTestAccountAsync());
        }

        var tasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - 100 concurrent requests across 10 accounts
        for (int i = 0; i < 100; i++)
        {
            var account = accounts[i % accounts.Count];
            tasks.Add(Client.GetAsync($"/api/accounts/{account.Id}"));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().OnlyContain(r => r.IsSuccessStatusCode);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
            "100 concurrent requests should complete within 5 seconds");

        var averageResponseTime = stopwatch.ElapsedMilliseconds / 100.0;
        averageResponseTime.Should().BeLessThan(100,
            "Average response time should be under 100ms");
    }

    [Test]
    public async Task CreateAccount_MultipleSimultaneousRequests_ShouldNotCreateDuplicates()
    {
        // Arrange
        await AuthenticateAsync();
        var customer = await CreateTestCustomerAsync();

        var createAccountTasks = new List<Task<HttpResponseMessage>>();

        // Act - Try to create 10 accounts simultaneously
        for (int i = 0; i < 10; i++)
        {
            var request = new CreateAccountRequest
            {
                CustomerId = customer.Id,
                AccountType = AccountType.Checking,
                InitialDeposit = 1000m + i,
                Currency = "USD"
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            createAccountTasks.Add(Client.PostAsync("/api/accounts", requestContent));
        }

        var responses = await Task.WhenAll(createAccountTasks);

        // Assert
        var successfulResponses = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        successfulResponses.Should().Be(10, "All account creation requests should succeed");

        // Verify in database
        var accountsInDb = await DbContext.Accounts
            .Where(a => a.CustomerId == customer.Id)
            .ToListAsync();

        accountsInDb.Should().HaveCount(10);

        // Verify all accounts have unique account numbers
        var accountNumbers = accountsInDb.Select(a => a.AccountNumber).ToList();
        accountNumbers.Should().OnlyHaveUniqueItems();
    }
}
```

### Database Performance Testing

```csharp
// ✅ Good: Database performance integration tests
[TestFixture]
public class DatabasePerformanceIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetTransactionHistory_LargeDataset_ShouldCompleteQuickly()
    {
        // Arrange
        var account = await CreateTestAccountAsync();

        // Create 10,000 transactions
        var transactions = new List<Transaction>();
        for (int i = 0; i < 10000; i++)
        {
            var transaction = Transaction.CreateDeposit(
                account.Id,
                new Money(100m + i, Currency.USD),
                $"Test transaction {i}");
            transactions.Add(transaction);
        }

        DbContext.Transactions.AddRange(transactions);
        await DbContext.SaveChangesAsync();

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await Client.GetAsync($"/api/accounts/{account.Id}/transactions?page=1&pageSize=50");

        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            "Query should complete within 500ms even with large dataset");

        var responseContent = await response.Content.ReadAsStringAsync();
        var pagedResult = JsonSerializer.Deserialize<PagedResult<TransactionDto>>(
            responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        pagedResult!.Data.Should().HaveCount(50);
        pagedResult.TotalCount.Should().Be(10000);
    }
}
```

## Security Integration Testing

### Authentication and Authorization Tests

```csharp
// ✅ Good: Security integration tests
[TestFixture]
public class SecurityIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task AccessProtectedEndpoint_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var account = await CreateTestAccountAsync();
        // No authentication header set

        // Act
        var response = await Client.GetAsync($"/api/accounts/{account.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AccessProtectedEndpoint_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var account = await CreateTestAccountAsync();
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await Client.GetAsync($"/api/accounts/{account.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AccessOtherUserAccount_ShouldReturnForbidden()
    {
        // Arrange
        await AuthenticateAsync("user1@example.com");
        var user1 = await CreateTestUserAsync("user1@example.com");
        var customer1 = await CreateTestCustomerAsync();
        var account1 = await CreateTestAccountAsync(customer1.Id);

        // Create another user's account
        var user2 = await CreateTestUserAsync("user2@example.com");
        var customer2 = await CreateTestCustomerAsync();
        var account2 = await CreateTestAccountAsync(customer2.Id);

        // Act - User 1 tries to access User 2's account
        var response = await Client.GetAsync($"/api/accounts/{account2.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

## Test Cleanup and Data Management

### Cleanup Strategies

```csharp
// ✅ Good: Proper test cleanup
public abstract class IntegrationTestBase : IAsyncLifetime
{
    // ... existing code ...

    protected async Task CleanupDatabaseAsync()
    {
        // Clean up in reverse dependency order
        DbContext.Transactions.RemoveRange(DbContext.Transactions);
        DbContext.Accounts.RemoveRange(DbContext.Accounts);
        DbContext.Customers.RemoveRange(DbContext.Customers);
        DbContext.Users.RemoveRange(DbContext.Users);

        await DbContext.SaveChangesAsync();
    }

    protected async Task ResetDatabaseAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
    }
}

// For tests that need isolation
[TestFixture]
public class IsolatedIntegrationTests : IntegrationTestBase
{
    [TearDown]
    public async Task TearDown()
    {
        await CleanupDatabaseAsync();
    }
}
```

## Integration Testing Checklist

### API Testing

- [ ] All endpoints tested with valid and invalid inputs
- [ ] Authentication and authorization scenarios covered
- [ ] Different content types tested (JSON, XML, PDF, etc.)
- [ ] Pagination and filtering tested
- [ ] Error responses validated
- [ ] Performance under load tested

### Database Testing

- [ ] Repository methods tested against real database
- [ ] Transaction behavior verified
- [ ] Concurrency scenarios tested
- [ ] Data integrity constraints verified
- [ ] Performance with large datasets tested

### External Services

- [ ] Service integration tested with mocks
- [ ] Error handling and retry logic verified
- [ ] Timeout scenarios tested
- [ ] Circuit breaker behavior validated

### Security Testing

- [ ] Authentication flows tested
- [ ] Authorization rules enforced
- [ ] Input validation working
- [ ] SQL injection prevention verified
- [ ] XSS prevention verified

### Performance Testing

- [ ] Response times under acceptable limits
- [ ] Concurrent request handling verified
- [ ] Memory usage reasonable
- [ ] Database query performance optimized

## CI/CD Pipeline Integration

### Azure DevOps Pipeline Configuration

Integration tests are automatically executed in the CI pipeline with proper test isolation and reporting:

```yaml
- task: DotNetCoreCLI@2
  displayName: "Run Integration Tests with Code Coverage"
  inputs:
    command: "test"
    projects: "src/services/Security/tests/Security.Infrastructure.IntegrationTests/Security.Infrastructure.IntegrationTests.csproj"
    arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage" --settings "$(Build.SourcesDirectory)/src/coverlet.runsettings"'
    publishTestResults: true
  # Note: TestContainers will automatically use Docker on Linux
  continueOnError: true
```

### Test Environment Requirements

- **Docker**: Required for TestContainers (automatically available on ubuntu-latest agents)
- **SQL Server**: Provided via TestContainers
- **Redis**: Provided via TestContainers
- **Test Data**: Isolated per test run

### Coverage Integration

Integration tests contribute to overall code coverage metrics:

```xml
<!-- coverlet.runsettings -->
<Configuration>
  <Format>cobertura,opencover</Format>
  <Exclude>[*.Tests]*,[*.UnitTests]*,[*.IntegrationTests]*</Exclude>
  <IncludeTestAssembly>false</IncludeTestAssembly>
</Configuration>
```

### CI Pipeline Best Practices

1. **Run After Unit Tests**: Integration tests run after unit tests pass
2. **Parallel Execution**: Can run in parallel with other services' tests
3. **Fail Fast**: Stop pipeline if critical integration tests fail
4. **Continue on Error**: Allow pipeline to continue for non-critical integration tests
5. **Timeout Management**: Set appropriate timeouts for database operations

## Summary

1. **Use real infrastructure** - Test against actual databases and services when possible
2. **Isolate test data** - Each test should have its own clean data set
3. **Test end-to-end scenarios** - Verify complete user workflows
4. **Mock external dependencies** - Use test doubles for third-party services
5. **Verify error handling** - Test failure scenarios thoroughly
6. **Test security controls** - Validate authentication and authorization
7. **Monitor performance** - Ensure acceptable response times
8. **Clean up properly** - Remove test data to prevent interference
9. **Use test containers** - Leverage Docker for consistent test environments
10. **Automate in CI/CD** - Run integration tests as part of build pipeline
