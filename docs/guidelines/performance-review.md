# Performance Review Guidelines

## Overview

This document provides comprehensive performance review guidelines for the Bank System Microservices project, focusing on identifying and addressing performance bottlenecks, optimizing resource usage, and ensuring scalability.

## Performance Review Framework

### Database Performance

#### Query Optimization

```csharp
// ✅ Good: Optimized database queries
public async Task<PagedResult<TransactionDto>> GetTransactionHistoryAsync(
    Guid accountId,
    int page,
    int pageSize,
    DateTime? fromDate = null,
    CancellationToken cancellationToken = default)
{
    // Use projection to select only needed fields
    var query = _context.Transactions
        .Where(t => t.AccountId == accountId)
        .Where(t => !fromDate.HasValue || t.Timestamp >= fromDate)
        .OrderByDescending(t => t.Timestamp)
        .Select(t => new TransactionDto
        {
            Id = t.Id,
            Amount = t.Amount,
            Description = t.Description,
            Timestamp = t.Timestamp,
            Type = t.Type
        });

    // Efficient pagination
    var totalCount = await query.CountAsync(cancellationToken);
    var transactions = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsNoTracking() // Read-only queries
        .ToListAsync(cancellationToken);

    return new PagedResult<TransactionDto>
    {
        Data = transactions,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}

// ❌ Bad: Inefficient queries
public async Task<List<TransactionDto>> GetTransactionHistory(Guid accountId)
{
    // Loads all transactions into memory
    var allTransactions = await _context.Transactions
        .Include(t => t.Account) // Unnecessary eager loading
        .Include(t => t.Account.Customer) // Even more unnecessary data
        .ToListAsync();

    // Filtering in memory instead of database
    return allTransactions
        .Where(t => t.AccountId == accountId)
        .OrderByDescending(t => t.Timestamp)
        .Take(50) // Still loads everything first
        .Select(t => _mapper.Map<TransactionDto>(t))
        .ToList();
}
```

#### Database Connection Management

```csharp
// ✅ Good: Proper connection configuration
services.AddDbContext<BankDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });

    // Performance configurations
    options.EnableSensitiveDataLogging(false); // Don't log data in production
    options.EnableDetailedErrors(false); // Disable in production
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(RelationalEventId.MultipleCollectionIncludeWarning));
});

// Connection pooling configuration
services.AddDbContextPool<BankDbContext>(options =>
{
    options.UseSqlServer(connectionString);
}, poolSize: 128); // Adjust based on load

// ❌ Bad: Poor connection management
services.AddDbContext<BankDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.EnableSensitiveDataLogging(true); // Performance impact
    options.EnableDetailedErrors(true); // Performance impact
    // No connection pooling
    // No retry configuration
    // No timeout configuration
});
```

#### Indexing Strategy

```sql
-- ✅ Good: Strategic indexes for common queries
-- Index for account transactions lookup
CREATE NONCLUSTERED INDEX IX_Transactions_AccountId_Timestamp
ON Transactions (AccountId, Timestamp DESC)
INCLUDE (Amount, Description, Type);

-- Index for customer account lookup
CREATE NONCLUSTERED INDEX IX_Accounts_CustomerId
ON Accounts (CustomerId)
INCLUDE (AccountNumber, Balance, Status);

-- Composite index for complex queries
CREATE NONCLUSTERED INDEX IX_Transactions_AccountId_Type_Timestamp
ON Transactions (AccountId, Type, Timestamp DESC)
WHERE Type IN ('Deposit', 'Withdrawal');

-- ❌ Bad: Missing or poorly designed indexes
-- No indexes on frequently queried columns
-- Over-indexing (indexes on every column)
-- Indexes without proper include columns
```

### Caching Strategy

#### Memory Caching

```csharp
// ✅ Good: Strategic caching implementation
public class AccountService
{
    private readonly IMemoryCache _cache;
    private readonly IAccountRepository _repository;
    private readonly ILogger<AccountService> _logger;

    private readonly MemoryCacheEntryOptions _accountCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
        SlidingExpiration = TimeSpan.FromMinutes(5),
        Priority = CacheItemPriority.High,
        Size = 1
    };

    public async Task<AccountDto?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"account:{accountId}";

        if (_cache.TryGetValue(cacheKey, out AccountDto? cachedAccount))
        {
            _logger.LogDebug("Account {AccountId} retrieved from cache", accountId);
            return cachedAccount;
        }

        var account = await _repository.GetByIdAsync(accountId, cancellationToken);
        if (account != null)
        {
            var accountDto = _mapper.Map<AccountDto>(account);
            _cache.Set(cacheKey, accountDto, _accountCacheOptions);
            _logger.LogDebug("Account {AccountId} cached", accountId);
            return accountDto;
        }

        return null;
    }

    public async Task InvalidateAccountCacheAsync(Guid accountId)
    {
        var cacheKey = $"account:{accountId}";
        _cache.Remove(cacheKey);
        _logger.LogDebug("Account {AccountId} cache invalidated", accountId);
    }
}

// Cache size configuration
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Limit cache size
    options.TrackStatistics = true; // Enable metrics
});

// ❌ Bad: Inappropriate caching
public class AccountService
{
    public async Task<AccountDto> GetAccount(Guid accountId)
    {
        // Always goes to database - no caching benefit
        return await _repository.GetByIdAsync(accountId);
    }

    // Or worse - caching everything indefinitely
    public async Task<AccountDto> GetAccountWithBadCaching(Guid accountId)
    {
        var cacheKey = $"account:{accountId}";
        if (_cache.TryGetValue(cacheKey, out AccountDto account))
            return account;

        account = await _repository.GetByIdAsync(accountId);
        _cache.Set(cacheKey, account); // No expiration - memory leak
        return account;
    }
}
```

#### Distributed Caching (Redis)

```csharp
// ✅ Good: Redis distributed caching
public class DistributedAccountCache
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedAccountCache> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public DistributedAccountCache(IDistributedCache distributedCache, ILogger<DistributedAccountCache> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<AccountSummaryDto?> GetAccountSummaryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"account-summary:{accountId}";

        try
        {
            var cachedData = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var accountSummary = JsonSerializer.Deserialize<AccountSummaryDto>(cachedData, _serializerOptions);
                _logger.LogDebug("Account summary {AccountId} retrieved from distributed cache", accountId);
                return accountSummary;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve account summary {AccountId} from cache", accountId);
            // Continue to fetch from source
        }

        return null;
    }

    public async Task SetAccountSummaryAsync(
        AccountSummaryDto accountSummary,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"account-summary:{accountSummary.Id}";
        var serializedData = JsonSerializer.Serialize(accountSummary, _serializerOptions);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        try
        {
            await _distributedCache.SetStringAsync(cacheKey, serializedData, cacheOptions, cancellationToken);
            _logger.LogDebug("Account summary {AccountId} cached in distributed cache", accountSummary.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache account summary {AccountId}", accountSummary.Id);
            // Non-critical failure - don't throw
        }
    }
}

// Redis configuration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = connectionString;
    options.InstanceName = "BankSystem";
});
```

### Async Programming Performance

#### Proper Async/Await Usage

```csharp
// ✅ Good: Efficient async operations
public class TransactionService
{
    public async Task<Result<BulkTransactionResult>> ProcessBulkTransactionsAsync(
        IEnumerable<CreateTransactionCommand> commands,
        CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(10, 10); // Limit concurrency
        var results = new ConcurrentBag<TransactionResult>();

        var tasks = commands.Select(async command =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await ProcessSingleTransactionAsync(command, cancellationToken);
                results.Add(result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return Result<BulkTransactionResult>.Success(new BulkTransactionResult
        {
            TotalProcessed = results.Count,
            Successful = results.Count(r => r.IsSuccess),
            Failed = results.Count(r => !r.IsSuccess)
        });
    }

    public async Task<Result<TransactionDto>> ProcessTransactionWithTimeoutAsync(
        CreateTransactionCommand command,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            return await ProcessSingleTransactionAsync(command, combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Transaction processing timed out after {Timeout}", timeout);
            return Result<TransactionDto>.Failure("Transaction processing timed out");
        }
    }
}

// ❌ Bad: Blocking async operations
public class TransactionService
{
    public TransactionDto ProcessTransaction(CreateTransactionCommand command)
    {
        // Blocking async call - can cause deadlocks
        var result = ProcessTransactionAsync(command).Result;
        return result.Value;
    }

    public async Task<List<TransactionDto>> ProcessMultipleTransactions(
        IEnumerable<CreateTransactionCommand> commands)
    {
        var results = new List<TransactionDto>();

        // Sequential processing - inefficient
        foreach (var command in commands)
        {
            var result = await ProcessTransactionAsync(command);
            results.Add(result.Value);
        }

        return results;
    }
}
```

#### ConfigureAwait Usage

```csharp
// ✅ Good: Proper ConfigureAwait usage in libraries
public class AccountRepository
{
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false); // Avoid deadlocks in library code
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

// In ASP.NET Core controllers, ConfigureAwait(false) is not needed
[ApiController]
public class AccountController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDto>> GetAccount(Guid id, CancellationToken cancellationToken)
    {
        // No ConfigureAwait needed in ASP.NET Core
        var account = await _accountService.GetByIdAsync(id, cancellationToken);
        return account != null ? Ok(account) : NotFound();
    }
}
```

### Memory Management

#### Object Allocation Optimization

```csharp
// ✅ Good: Efficient memory usage
public class TransactionProcessor
{
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    public TransactionProcessor(ObjectPool<StringBuilder> stringBuilderPool)
    {
        _stringBuilderPool = stringBuilderPool;
    }

    public string GenerateTransactionSummary(IEnumerable<Transaction> transactions)
    {
        var sb = _stringBuilderPool.Get();
        try
        {
            foreach (var transaction in transactions)
            {
                sb.AppendLine($"{transaction.Timestamp:yyyy-MM-dd}: {transaction.Amount:C}");
            }
            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    // Use spans for string operations
    public bool IsValidAccountNumber(ReadOnlySpan<char> accountNumber)
    {
        if (accountNumber.Length != 10)
            return false;

        foreach (var c in accountNumber)
        {
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }
}

// Object pool configuration
services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
services.AddSingleton(serviceProvider =>
{
    var provider = serviceProvider.GetRequiredService<ObjectPoolProvider>();
    return provider.CreateStringBuilderPool();
});

// ❌ Bad: Inefficient memory allocation
public class TransactionProcessor
{
    public string GenerateTransactionSummary(IEnumerable<Transaction> transactions)
    {
        var summary = string.Empty;
        foreach (var transaction in transactions)
        {
            // Creates new string objects on each concatenation
            summary += $"{transaction.Timestamp:yyyy-MM-dd}: {transaction.Amount:C}\n";
        }
        return summary;
    }

    public bool IsValidAccountNumber(string accountNumber)
    {
        // Unnecessary string allocations
        return accountNumber.Length == 10 && accountNumber.All(char.IsDigit);
    }
}
```

#### IDisposable Implementation

```csharp
// ✅ Good: Proper resource disposal
public class TransactionExportService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly MemoryStream _buffer;
    private bool _disposed = false;

    public TransactionExportService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _buffer = new MemoryStream();
    }

    public async Task<byte[]> ExportTransactionsAsync(
        IEnumerable<Transaction> transactions,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _buffer.SetLength(0);
        using var writer = new StreamWriter(_buffer, leaveOpen: true);

        foreach (var transaction in transactions)
        {
            await writer.WriteLineAsync($"{transaction.Id},{transaction.Amount},{transaction.Timestamp}");
        }

        await writer.FlushAsync();
        return _buffer.ToArray();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _buffer?.Dispose();
            _disposed = true;
        }
    }
}

// ❌ Bad: Resource leaks
public class TransactionExportService
{
    public async Task<byte[]> ExportTransactions(IEnumerable<Transaction> transactions)
    {
        var httpClient = new HttpClient(); // Not disposed
        var stream = new MemoryStream(); // Not disposed

        // Process transactions without proper cleanup
        foreach (var transaction in transactions)
        {
            var response = await httpClient.GetAsync($"api/validate/{transaction.Id}");
            // Response not disposed
        }

        return stream.ToArray();
    }
}
```

### HTTP Performance

#### HTTP Client Configuration

```csharp
// ✅ Good: Optimized HttpClient configuration
services.AddHttpClient<ExternalBankService>(client =>
{
    client.BaseAddress = new Uri("https://api.external-bank.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "BankSystem/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    MaxConnectionsPerServer = 20,
    EnableMultipleHttp2Connections = true
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var logger = context.GetLogger();
                logger?.LogWarning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
            });
}

// ❌ Bad: Poor HttpClient usage
public class ExternalBankService
{
    public async Task<string> GetAccountInfoAsync(string accountId)
    {
        // Creating new HttpClient for each request
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.external-bank.com/");

        // No timeout configuration
        // No retry policy
        // No connection pooling benefits

        var response = await client.GetAsync($"accounts/{accountId}");
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Response Compression

```csharp
// ✅ Good: Response compression configuration
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "text/json"
    });
});

services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// Enable compression in pipeline
app.UseResponseCompression();
```

## Performance Monitoring

### Application Performance Monitoring

```csharp
// ✅ Good: Performance monitoring setup
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly DiagnosticSource _diagnosticSource;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger,
        DiagnosticSource diagnosticSource)
    {
        _next = next;
        _logger = logger;
        _diagnosticSource = diagnosticSource;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            // Log slow requests
            if (elapsed > 1000)
            {
                _logger.LogWarning("Slow request detected: {Path} took {ElapsedMs}ms",
                    requestPath, elapsed);
            }

            // Send metrics to monitoring system
            _diagnosticSource.Write("RequestPerformance", new
            {
                Path = requestPath.Value,
                Method = context.Request.Method,
                StatusCode = context.Response.StatusCode,
                ElapsedMilliseconds = elapsed
            });
        }
    }
}

// Custom performance counters
public class PerformanceCounters
{
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly UpDownCounter<int> _activeConnections;

    public PerformanceCounters(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("BankSystem.Performance");

        _requestCounter = meter.CreateCounter<long>(
            "http_requests_total",
            description: "Total number of HTTP requests");

        _requestDuration = meter.CreateHistogram<double>(
            "http_request_duration_seconds",
            description: "Duration of HTTP requests");

        _activeConnections = meter.CreateUpDownCounter<int>(
            "http_active_connections",
            description: "Number of active HTTP connections");
    }

    public void RecordRequest(string method, string path, int statusCode, double durationSeconds)
    {
        _requestCounter.Add(1,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("path", path),
            new KeyValuePair<string, object?>("status_code", statusCode));

        _requestDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("path", path));
    }
}
```

### Database Performance Monitoring

```csharp
// ✅ Good: Database performance tracking
public class DatabasePerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger<DatabasePerformanceInterceptor> _logger;
    private readonly DiagnosticSource _diagnosticSource;

    public DatabasePerformanceInterceptor(
        ILogger<DatabasePerformanceInterceptor> logger,
        DiagnosticSource diagnosticSource)
    {
        _logger = logger;
        _diagnosticSource = diagnosticSource;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var duration = eventData.Duration.TotalMilliseconds;

        // Log slow queries
        if (duration > 1000)
        {
            _logger.LogWarning("Slow database query detected: {CommandText} took {Duration}ms",
                command.CommandText, duration);
        }

        // Send metrics
        _diagnosticSource.Write("DatabaseQuery", new
        {
            CommandText = command.CommandText,
            Duration = duration,
            RecordsAffected = result.RecordsAffected
        });

        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}

// Register interceptor
services.AddDbContext<BankDbContext>(options =>
{
    options.UseSqlServer(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<DatabasePerformanceInterceptor>());
});
```

## Performance Testing

### Load Testing Configuration

```csharp
// ✅ Good: Performance test setup
[TestFixture]
public class PerformanceTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Use in-memory database for consistent performance testing
                    services.RemoveDbContext<BankDbContext>();
                    services.AddDbContext<BankDbContext>(options =>
                        options.UseInMemoryDatabase("PerformanceTestDb"));
                });
            });

        _client = _factory.CreateClient();
    }

    [Test]
    public async Task GetAccountTransactions_Under100ConcurrentRequests_ShouldMaintainPerformance()
    {
        // Arrange
        var accountId = await CreateTestAccountAsync();
        await SeedTransactionsAsync(accountId, 1000); // Create test data

        var tasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate 100 concurrent requests
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_client.GetAsync($"/api/accounts/{accountId}/transactions?page=1&pageSize=50"));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.That(responses.All(r => r.IsSuccessStatusCode), Is.True);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000),
            "100 concurrent requests should complete within 5 seconds");

        var averageResponseTime = stopwatch.ElapsedMilliseconds / 100.0;
        Assert.That(averageResponseTime, Is.LessThan(100),
            "Average response time should be under 100ms");
    }

    [Test]
    public async Task CreateTransaction_MemoryUsage_ShouldNotExceedThreshold()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
        var accountId = await CreateTestAccountAsync();

        // Act - Create many transactions
        for (int i = 0; i < 1000; i++)
        {
            var request = new CreateTransactionRequest
            {
                AccountId = accountId,
                Amount = 100,
                Description = $"Test transaction {i}"
            };

            var response = await _client.PostAsJsonAsync("/api/transactions", request);
            Assert.That(response.IsSuccessStatusCode, Is.True);
        }

        // Force garbage collection and measure memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert - Memory increase should be reasonable
        Assert.That(memoryIncrease, Is.LessThan(50 * 1024 * 1024),
            "Memory increase should be less than 50MB for 1000 transactions");
    }
}
```

## Performance Review Checklist

### Database Performance

- [ ] Queries use appropriate indexes
- [ ] N+1 query problems avoided
- [ ] Pagination implemented for large result sets
- [ ] Projection used to select only needed fields
- [ ] AsNoTracking() used for read-only queries
- [ ] Connection pooling configured
- [ ] Query timeouts set appropriately

### Caching Strategy

- [ ] Appropriate cache levels implemented
- [ ] Cache expiration policies defined
- [ ] Cache invalidation strategy in place
- [ ] Cache hit/miss ratios monitored
- [ ] Memory usage within limits

### Async Programming

- [ ] Async/await used for I/O operations
- [ ] ConfigureAwait(false) in library code
- [ ] Deadlock prevention measures
- [ ] Cancellation tokens used
- [ ] Concurrent operations optimized

### Memory Management

- [ ] IDisposable implemented where needed
- [ ] Object pooling for frequently allocated objects
- [ ] String concatenation optimized
- [ ] Large object heap pressure minimized
- [ ] Memory leaks prevented

### HTTP Performance

- [ ] HttpClient properly configured
- [ ] Connection pooling enabled
- [ ] Response compression configured
- [ ] Appropriate timeout values
- [ ] Retry policies implemented

### Monitoring

- [ ] Performance metrics collected
- [ ] Slow operations logged
- [ ] Resource usage monitored
- [ ] Alerting configured for thresholds
- [ ] Performance baselines established

## Summary

1. **Optimize database queries** - Use indexes, projection, and pagination
2. **Implement caching strategies** - Memory and distributed caching
3. **Use async programming correctly** - Avoid blocking calls
4. **Manage memory efficiently** - Dispose resources, use pooling
5. **Configure HTTP clients properly** - Connection pooling and timeouts
6. **Monitor performance continuously** - Metrics and logging
7. **Test performance regularly** - Load and stress testing
8. **Set performance budgets** - Define acceptable thresholds
9. **Profile regularly** - Identify bottlenecks early
10. **Scale appropriately** - Horizontal and vertical scaling strategies
