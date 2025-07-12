# Async/Await Best Practices

## Overview

Asynchronous programming is essential for building scalable applications. This guideline provides best practices for using async/await patterns in the Bank System Microservices project.

## Fundamental Principles

### Always Use Async/Await for I/O Operations

```csharp
// ✅ Good: Async for I/O operations
public async Task<Result<Account>> CreateAccountAsync(CreateAccountCommand command)
{
    var existingAccount = await _repository.GetByAccountNumberAsync(command.AccountNumber);

    if (existingAccount != null)
        return Result<Account>.Failure("Account number already exists");

    var account = Account.CreateNew(command.AccountNumber, command.CustomerId);
    await _repository.AddAsync(account);

    return Result<Account>.Success(account);
}

// ❌ Bad: Blocking async calls
public Account CreateAccount(CreateAccountCommand command)
{
    var result = CreateAccountAsync(command).Result; // Don't do this!
    return result.Value;
}
```

### Never Block on Async Code

```csharp
// ❌ Bad: Blocking patterns that can cause deadlocks
public void ProcessTransaction(TransactionCommand command)
{
    var result = ProcessTransactionAsync(command).Result;
    var account = GetAccountAsync(command.AccountId).GetAwaiter().GetResult();
    ProcessAsync(command).Wait();
}

// ✅ Good: Async all the way
public async Task ProcessTransactionAsync(TransactionCommand command)
{
    var result = await ProcessTransactionInternalAsync(command);
    var account = await GetAccountAsync(command.AccountId);
    await ProcessInternalAsync(command);
}
```

## ConfigureAwait Guidelines

### Use ConfigureAwait(false) in Libraries

```csharp
// ✅ Good: ConfigureAwait(false) in library code
public class AccountService
{
    public async Task<Account> GetAccountAsync(Guid accountId)
    {
        var account = await _repository.GetByIdAsync(accountId).ConfigureAwait(false);

        if (account == null)
            throw new AccountNotFoundException(accountId);

        return account;
    }
}

// ✅ Good: Don't use ConfigureAwait in ASP.NET Core controllers
[ApiController]
public class AccountController : ControllerBase
{
    [HttpGet("{accountId}")]
    public async Task<ActionResult<AccountDto>> GetAccount(Guid accountId)
    {
        // Don't use ConfigureAwait(false) here - we need the context
        var account = await _accountService.GetAccountAsync(accountId);
        return Ok(_mapper.Map<AccountDto>(account));
    }
}
```

## Parallel Async Operations

### Task.WhenAll for Independent Operations

```csharp
// ✅ Good: Parallel execution of independent operations
public async Task<Result> ProcessMultipleTransactionsAsync(List<TransactionCommand> commands)
{
    var tasks = commands.Select(cmd => ProcessTransactionAsync(cmd));
    var results = await Task.WhenAll(tasks);

    return results.All(r => r.IsSuccess)
        ? Result.Success()
        : Result.Failure("One or more transactions failed");
}

// ✅ Good: Parallel data fetching
public async Task<AccountSummaryDto> GetAccountSummaryAsync(Guid accountId)
{
    var accountTask = _accountRepository.GetByIdAsync(accountId);
    var transactionsTask = _transactionRepository.GetRecentByAccountIdAsync(accountId, 10);
    var balanceHistoryTask = _balanceRepository.GetHistoryAsync(accountId, TimeSpan.FromDays(30));

    await Task.WhenAll(accountTask, transactionsTask, balanceHistoryTask);

    return new AccountSummaryDto
    {
        Account = _mapper.Map<AccountDto>(accountTask.Result),
        RecentTransactions = _mapper.Map<List<TransactionDto>>(transactionsTask.Result),
        BalanceHistory = _mapper.Map<List<BalanceHistoryDto>>(balanceHistoryTask.Result)
    };
}
```

### Task.WhenAny for Timeout Scenarios

```csharp
// ✅ Good: Implementing timeout with Task.WhenAny
public async Task<Result<PaymentResponse>> ProcessPaymentWithTimeoutAsync(
    PaymentRequest request,
    TimeSpan timeout)
{
    var paymentTask = _paymentService.ProcessPaymentAsync(request);
    var timeoutTask = Task.Delay(timeout);

    var completedTask = await Task.WhenAny(paymentTask, timeoutTask);

    if (completedTask == timeoutTask)
    {
        return Result<PaymentResponse>.Failure("Payment processing timed out");
    }

    return await paymentTask;
}
```

## Cancellation Token Usage

### Always Support Cancellation

```csharp
// ✅ Good: Proper cancellation token usage
public async Task<Result<TransactionDto>> ProcessTransactionAsync(
    CreateTransactionCommand command,
    CancellationToken cancellationToken = default)
{
    // Check cancellation at the beginning
    cancellationToken.ThrowIfCancellationRequested();

    var account = await _accountRepository.GetByIdAsync(
        command.AccountId, cancellationToken);

    if (account == null)
        return Result<TransactionDto>.Failure("Account not found");

    // Check cancellation before expensive operations
    cancellationToken.ThrowIfCancellationRequested();

    var validationResult = await ValidateTransactionAsync(command, cancellationToken);
    if (!validationResult.IsSuccess)
        return Result<TransactionDto>.Failure(validationResult.Error);

    // Pass cancellation token to all async calls
    var transaction = await CreateTransactionAsync(account, command, cancellationToken);
    await _eventPublisher.PublishAsync(transaction.DomainEvents, cancellationToken);

    return Result<TransactionDto>.Success(_mapper.Map<TransactionDto>(transaction));
}
```

### Cancellation in Loops

```csharp
// ✅ Good: Check cancellation in loops
public async Task ProcessBatchAsync(
    IEnumerable<TransactionCommand> commands,
    CancellationToken cancellationToken = default)
{
    foreach (var command in commands)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await ProcessTransactionAsync(command, cancellationToken);

        // Optional: Add delay to prevent overwhelming the system
        await Task.Delay(100, cancellationToken);
    }
}
```

## Exception Handling in Async Code

### Proper Exception Propagation

```csharp
// ✅ Good: Proper exception handling in async methods
public async Task<Result<AccountDto>> GetAccountWithRetryAsync(Guid accountId)
{
    const int maxRetries = 3;
    var retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            return account != null
                ? Result<AccountDto>.Success(_mapper.Map<AccountDto>(account))
                : Result<AccountDto>.Failure("Account not found");
        }
        catch (SqlException ex) when (IsTransientError(ex))
        {
            retryCount++;
            if (retryCount >= maxRetries)
                throw;

            var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
            await Task.Delay(delay);
        }
    }

    return Result<AccountDto>.Failure("Failed to retrieve account after retries");
}

private static bool IsTransientError(SqlException ex)
{
    // Check for transient error codes
    return ex.Number is 2 or 53 or 121 or 1205;
}
```

### AggregateException Handling

```csharp
// ✅ Good: Handle AggregateException properly
public async Task<Result> ProcessBatchWithErrorHandlingAsync(
    List<TransactionCommand> commands)
{
    try
    {
        var tasks = commands.Select(ProcessTransactionAsync);
        await Task.WhenAll(tasks);
        return Result.Success();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing transaction batch");

        // If using Task.WhenAll, individual exceptions are wrapped in AggregateException
        if (ex is AggregateException aggEx)
        {
            var innerExceptions = aggEx.InnerExceptions;
            var errorMessage = string.Join("; ", innerExceptions.Select(e => e.Message));
            return Result.Failure($"Batch processing failed: {errorMessage}");
        }

        return Result.Failure($"Batch processing failed: {ex.Message}");
    }
}
```

## Async Enumerable (IAsyncEnumerable)

### Streaming Large Data Sets

```csharp
// ✅ Good: Use IAsyncEnumerable for streaming
public async IAsyncEnumerable<TransactionDto> GetTransactionStreamAsync(
    Guid accountId,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    const int batchSize = 100;
    var offset = 0;

    while (true)
    {
        var transactions = await _transactionRepository.GetBatchAsync(
            accountId, offset, batchSize, cancellationToken);

        if (!transactions.Any())
            yield break;

        foreach (var transaction in transactions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return _mapper.Map<TransactionDto>(transaction);
        }

        offset += batchSize;

        if (transactions.Count() < batchSize)
            yield break;
    }
}
```

## Background Services and Async

### Proper Background Service Implementation

```csharp
// ✅ Good: Background service with proper async handling
public class TransactionProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionProcessingService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var transactionQueue = scope.ServiceProvider
                    .GetRequiredService<ITransactionQueue>();

                var pendingTransactions = await transactionQueue
                    .DequeueBatchAsync(10, stoppingToken);

                if (pendingTransactions.Any())
                {
                    await ProcessTransactionBatchAsync(pendingTransactions, stoppingToken);
                }
                else
                {
                    // Wait before checking again
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction processing service");

                // Wait before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task ProcessTransactionBatchAsync(
        IEnumerable<TransactionCommand> commands,
        CancellationToken cancellationToken)
    {
        var tasks = commands.Select(cmd =>
            ProcessSingleTransactionAsync(cmd, cancellationToken));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction batch");
            // Individual errors are already logged in ProcessSingleTransactionAsync
        }
    }
}
```

## Testing Async Code

### Unit Testing Async Methods

```csharp
public class AccountServiceTests
{
    [Fact]
    public async Task CreateAccountAsync_ValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            CustomerId = Guid.NewGuid(),
            AccountNumber = "123456789",
            InitialDeposit = 1000m
        };

        var mockRepository = new Mock<IAccountRepository>();
        mockRepository.Setup(r => r.GetByAccountNumberAsync(command.AccountNumber))
            .ReturnsAsync((Account?)null);

        var service = new AccountService(mockRepository.Object);

        // Act
        var result = await service.CreateAccountAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
    }

    [Fact]
    public async Task GetAccountAsync_NonExistentAccount_ShouldThrowException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var mockRepository = new Mock<IAccountRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        var service = new AccountService(mockRepository.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AccountNotFoundException>(
            () => service.GetAccountAsync(accountId));

        Assert.Equal(accountId, ex.AccountId);
    }
}
```

### Integration Testing with Async

```csharp
public class TransactionControllerIntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TransactionControllerIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateTransaction_ValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            AccountId = Guid.NewGuid(),
            Amount = 100m,
            Description = "Test transaction"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TransactionDto>(content);

        Assert.NotNull(result);
    }

    // Proper cleanup for test resources
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }
}
```

## Common Anti-Patterns to Avoid

### Don't Use .Result or .Wait()

```csharp
// ❌ Bad: Can cause deadlocks
public void ProcessTransaction(TransactionCommand command)
{
    var result = ProcessTransactionAsync(command).Result;
    var account = GetAccountAsync(command.AccountId).GetAwaiter().GetResult();
}

// ✅ Good: Async all the way
public async Task ProcessTransactionAsync(TransactionCommand command)
{
    var result = await ProcessTransactionInternalAsync(command);
    var account = await GetAccountAsync(command.AccountId);
}
```

### Don't Create Unnecessary Tasks

```csharp
// ❌ Bad: Unnecessary Task.Run
public async Task<Account> GetAccountAsync(Guid accountId)
{
    return await Task.Run(() => _repository.GetByIdAsync(accountId));
}

// ✅ Good: Direct async call
public async Task<Account> GetAccountAsync(Guid accountId)
{
    return await _repository.GetByIdAsync(accountId);
}
```

### Don't Fire and Forget Without Handling Exceptions

```csharp
// ❌ Bad: Fire and forget without exception handling
public async Task ProcessTransactionAsync(TransactionCommand command)
{
    // This exception will be lost
    _ = SendNotificationAsync(command.CustomerId);

    await ProcessTransactionInternalAsync(command);
}

// ✅ Good: Proper fire and forget with exception handling
public async Task ProcessTransactionAsync(TransactionCommand command)
{
    // Fire and forget with proper exception handling
    _ = Task.Run(async () =>
    {
        try
        {
            await SendNotificationAsync(command.CustomerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for customer {CustomerId}",
                command.CustomerId);
        }
    });

    await ProcessTransactionInternalAsync(command);
}
```

## Performance Considerations

### Use ValueTask When Appropriate

```csharp
// ✅ Good: Use ValueTask for frequently called methods that may complete synchronously
public ValueTask<Account?> GetCachedAccountAsync(Guid accountId)
{
    if (_cache.TryGetValue(accountId, out Account cachedAccount))
    {
        return new ValueTask<Account?>(cachedAccount);
    }

    return new ValueTask<Account?>(_repository.GetByIdAsync(accountId));
}
```

### Minimize Async State Machine Overhead

```csharp
// ✅ Good: Minimize async state machines
public Task<Account?> GetAccountIfExistsAsync(Guid accountId)
{
    // No need for async/await if just returning the task
    return _repository.GetByIdAsync(accountId);
}

// ✅ Good: Use async when you need to process the result
public async Task<Result<Account>> GetValidatedAccountAsync(Guid accountId)
{
    var account = await _repository.GetByIdAsync(accountId);

    if (account == null)
        return Result<Account>.Failure("Account not found");

    return Result<Account>.Success(account);
}
```

---

_This guideline follows the principles outlined in [Clean Code Guidelines](./clean-code.md) and [SOLID Principles](./solid-principles.md)._
