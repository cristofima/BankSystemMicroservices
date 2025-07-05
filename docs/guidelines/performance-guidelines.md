# Performance Guidelines

## Overview

This document provides comprehensive performance guidelines for the Bank System Microservices project, focusing on .NET 9 performance optimizations, efficient resource usage, and scalability best practices.

## Memory Management

### String Operations

```csharp
// ✅ Good: Use StringBuilder for multiple string operations
public string BuildTransactionSummary(IEnumerable<Transaction> transactions)
{
    var sb = new StringBuilder();
    foreach (var transaction in transactions)
    {
        sb.AppendLine($"{transaction.Date:yyyy-MM-dd}: {transaction.Amount:C} - {transaction.Description}");
    }
    return sb.ToString();
}

// ✅ Good: Use string interpolation for simple concatenations
public string FormatAccountInfo(Account account)
{
    return $"Account {account.AccountNumber}: Balance {account.Balance:C}";
}

// ❌ Bad: String concatenation in loops
public string BadBuildSummary(IEnumerable<Transaction> transactions)
{
    string summary = "";
    foreach (var transaction in transactions)
    {
        summary += $"{transaction.Date:yyyy-MM-dd}: {transaction.Amount:C}\n"; // Creates new string each iteration
    }
    return summary;
}
```

### Collection Operations

```csharp
// ✅ Good: Use appropriate collection types and sizes
public class TransactionService
{
    // Use List<T> with known capacity to avoid resizing
    public List<Transaction> ProcessBatch(int expectedCount)
    {
        var results = new List<Transaction>(expectedCount);
        // Process transactions...
        return results;
    }

    // Use HashSet for unique lookups
    private readonly HashSet<string> _processedTransactionIds = new();

    // Use Dictionary for key-value operations
    private readonly Dictionary<Guid, Account> _accountCache = new();
}

// ✅ Good: Use LINQ efficiently
public async Task<IEnumerable<Account>> GetActiveAccountsWithHighBalanceAsync()
{
    return await _context.Accounts
        .Where(a => a.Status == AccountStatus.Active)
        .Where(a => a.Balance.Amount > 10000)
        .OrderByDescending(a => a.Balance.Amount)
        .Take(100)
        .ToListAsync();
}

// ❌ Bad: Multiple LINQ operations that force enumeration
public IEnumerable<Account> BadGetActiveAccounts()
{
    var accounts = GetAllAccounts().ToList(); // Forces enumeration
    var active = accounts.Where(a => a.Status == AccountStatus.Active).ToList(); // Forces enumeration again
    var filtered = active.Where(a => a.Balance.Amount > 10000).ToList(); // Forces enumeration again
    return filtered.OrderByDescending(a => a.Balance.Amount).Take(100);
}
```

### Object Pooling

```csharp
// ✅ Good: Use ArrayPool for large arrays
public class CsvReportGenerator
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    public async Task<byte[]> GenerateReportAsync(IEnumerable<Transaction> transactions)
    {
        const int bufferSize = 1024 * 1024; // 1MB
        var buffer = _arrayPool.Rent(bufferSize);

        try
        {
            using var stream = new MemoryStream(buffer);
            await WriteCsvDataAsync(stream, transactions);
            return stream.ToArray();
        }
        finally
        {
            _arrayPool.Return(buffer);
        }
    }
}

// ✅ Good: Use ObjectPool for expensive objects
public class EmailService
{
    private readonly ObjectPool<MailMessage> _mailMessagePool;

    public EmailService(ObjectPool<MailMessage> mailMessagePool)
    {
        _mailMessagePool = mailMessagePool;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var message = _mailMessagePool.Get();
        try
        {
            message.To.Clear();
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;

            await SendMessageAsync(message);
        }
        finally
        {
            _mailMessagePool.Return(message);
        }
    }
}
```

## Asynchronous Programming Performance

### Cancellation Tokens

```csharp
// ✅ Good: Use cancellation tokens for long-running operations
public async Task<Result> ProcessLongRunningOperationAsync(CancellationToken cancellationToken)
{
    for (int i = 0; i < 1000; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await ProcessItemAsync(i, cancellationToken);
    }

    return Result.Success();
}

// ✅ Good: Configure timeouts for external calls
public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

    try
    {
        var response = await _httpClient.PostAsJsonAsync("/payments", request, cts.Token);
        return await response.Content.ReadFromJsonAsync<PaymentResult>();
    }
    catch (OperationCanceledException)
    {
        return PaymentResult.Timeout();
    }
}
```

### ConfigureAwait Usage

```csharp
// ✅ Good: Use ConfigureAwait(false) in library code
public async Task<Account> GetAccountAsync(Guid accountId)
{
    var account = await _repository.GetByIdAsync(accountId).ConfigureAwait(false);

    if (account == null)
        return null;

    await LoadTransactionsAsync(account).ConfigureAwait(false);
    return account;
}

// ✅ Good: Don't use ConfigureAwait(false) in ASP.NET Core controllers
[HttpGet("{accountId}")]
public async Task<ActionResult<AccountDto>> GetAccount(Guid accountId)
{
    // ASP.NET Core doesn't have SynchronizationContext, so ConfigureAwait(false) is unnecessary
    var account = await _accountService.GetAccountAsync(accountId);
    return Ok(_mapper.Map<AccountDto>(account));
}
```

### Parallel Processing

```csharp
// ✅ Good: Use parallel processing for independent operations
public async Task<BulkOperationResult> ProcessBulkTransactionsAsync(
    IEnumerable<CreateTransactionCommand> commands,
    CancellationToken cancellationToken)
{
    var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);
    var tasks = commands.Select(async command =>
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await ProcessTransactionAsync(command, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    });

    var results = await Task.WhenAll(tasks);

    return new BulkOperationResult
    {
        TotalProcessed = results.Length,
        Successful = results.Count(r => r.IsSuccess),
        Failed = results.Count(r => !r.IsSuccess)
    };
}

// ✅ Good: Use Parallel.ForEach for CPU-bound operations
public void ProcessLargeDataSet(IEnumerable<DataItem> items)
{
    var parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount,
        CancellationToken = CancellationToken.None
    };

    Parallel.ForEach(items, parallelOptions, item =>
    {
        ProcessItem(item);
    });
}
```

## Database Performance

### Entity Framework Optimization

```csharp
// ✅ Good: Use AsNoTracking for read-only queries
public async Task<IEnumerable<TransactionDto>> GetTransactionHistoryAsync(Guid accountId)
{
    return await _context.Transactions
        .AsNoTracking() // Improves performance for read-only data
        .Where(t => t.AccountId == accountId)
        .OrderByDescending(t => t.CreatedAt)
        .Take(100)
        .Select(t => new TransactionDto
        {
            Id = t.Id,
            Amount = t.Amount,
            Type = t.Type.ToString(),
            Date = t.CreatedAt
        })
        .ToListAsync();
}

// ✅ Good: Use projection to select only needed columns
public async Task<IEnumerable<AccountSummaryDto>> GetAccountSummariesAsync()
{
    return await _context.Accounts
        .AsNoTracking()
        .Select(a => new AccountSummaryDto
        {
            Id = a.Id,
            AccountNumber = a.AccountNumber,
            Balance = a.Balance.Amount,
            Status = a.Status.ToString()
        })
        .ToListAsync();
}

// ✅ Good: Use Include() carefully and specifically
public async Task<Account> GetAccountWithRecentTransactionsAsync(Guid accountId)
{
    return await _context.Accounts
        .Include(a => a.Transactions.OrderByDescending(t => t.CreatedAt).Take(10))
        .FirstOrDefaultAsync(a => a.Id == accountId);
}

// ❌ Bad: Loading unnecessary data
public async Task<Account> BadGetAccount(Guid accountId)
{
    // Loads all transactions for the account, could be thousands
    return await _context.Accounts
        .Include(a => a.Transactions)
        .Include(a => a.Customer)
        .Include(a => a.Customer.Addresses)
        .FirstOrDefaultAsync(a => a.Id == accountId);
}
```

### Connection Management

```csharp
// ✅ Good: Use connection pooling properly
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int MaxPoolSize { get; set; } = 100;
    public int MinPoolSize { get; set; } = 5;
    public int ConnectionLifetime { get; set; } = 0; // 0 = no limit
    public int CommandTimeout { get; set; } = 30;
}

// ✅ Good: Use proper connection string settings
public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, DatabaseOptions options)
    {
        services.AddDbContext<BankDbContext>(dbOptions =>
        {
            dbOptions.UseSqlServer(options.ConnectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(options.CommandTimeout);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        });

        return services;
    }
}
```

### Batch Operations

```csharp
// ✅ Good: Use batch operations for multiple database changes
public async Task<Result> ProcessMultipleTransactionsAsync(
    IEnumerable<Transaction> transactions,
    CancellationToken cancellationToken)
{
    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

    try
    {
        // Add all transactions in batch
        _context.Transactions.AddRange(transactions);

        // Save all changes in single round trip
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(cancellationToken);
        return Result.Failure($"Batch processing failed: {ex.Message}");
    }
}

// ✅ Good: Use bulk operations for large datasets
public async Task BulkUpdateAccountStatusAsync(IEnumerable<Guid> accountIds, AccountStatus newStatus)
{
    await _context.Accounts
        .Where(a => accountIds.Contains(a.Id))
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(a => a.Status, newStatus)
            .SetProperty(a => a.UpdatedAt, DateTime.UtcNow));
}
```

## Caching Strategies

### Memory Caching

```csharp
// ✅ Good: Use memory caching for frequently accessed data
public class AccountService
{
    private readonly IMemoryCache _cache;
    private readonly IAccountRepository _repository;

    public async Task<Account> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var cacheKey = $"account_{accountId}";

        if (_cache.TryGetValue(cacheKey, out Account cachedAccount))
        {
            return cachedAccount;
        }

        var account = await _repository.GetByIdAsync(accountId, cancellationToken);

        if (account != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(cacheKey, account, cacheOptions);
        }

        return account;
    }
}
```

### Distributed Caching

```csharp
// ✅ Good: Use distributed caching for scalability
public class DistributedAccountService
{
    private readonly IDistributedCache _distributedCache;
    private readonly JsonSerializerOptions _jsonOptions;

    public async Task<Account?> GetAccountAsync(Guid accountId)
    {
        var cacheKey = $"account_{accountId}";
        var cachedData = await _distributedCache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<Account>(cachedData, _jsonOptions);
        }

        var account = await _repository.GetByIdAsync(accountId);

        if (account != null)
        {
            var serializedAccount = JsonSerializer.Serialize(account, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            };

            await _distributedCache.SetStringAsync(cacheKey, serializedAccount, options);
        }

        return account;
    }
}
```

## HTTP Client Performance

### Connection Pooling

```csharp
// ✅ Good: Use HttpClientFactory for connection pooling
public class ExternalPaymentService
{
    private readonly HttpClient _httpClient;

    public ExternalPaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/payments", request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PaymentResult>();
    }
}

// Register in Program.cs
builder.Services.AddHttpClient<ExternalPaymentService>(client =>
{
    client.BaseAddress = new Uri("https://api.paymentprovider.com");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "BankSystem/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    MaxConnectionsPerServer = 100 // Increase connection pool size
});
```

### Response Compression

```csharp
// ✅ Good: Enable response compression
// Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

app.UseResponseCompression();
```

## JSON Performance

### System.Text.Json Optimization

```csharp
// ✅ Good: Configure JsonSerializer for performance
public static class JsonConfig
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false, // Smaller payload
        PropertyNameCaseInsensitive = true
    };

    public static readonly JsonSerializerOptions HighPerformanceOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        PropertyNameCaseInsensitive = false, // Faster parsing
        IncludeFields = false,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
}

// ✅ Good: Use source generators for AOT compilation
[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AccountDto))]
[JsonSerializable(typeof(TransactionDto))]
[JsonSerializable(typeof(List<TransactionDto>))]
internal partial class BankingJsonContext : JsonSerializerContext
{
}
```

## Performance Monitoring

### Custom Metrics

```csharp
// ✅ Good: Add performance metrics
public class TransactionService
{
    private readonly ILogger<TransactionService> _logger;
    private readonly Counter<int> _transactionCounter;
    private readonly Histogram<double> _transactionDuration;

    public TransactionService(ILogger<TransactionService> logger, IMeterFactory meterFactory)
    {
        _logger = logger;
        var meter = meterFactory.Create("BankSystem.Transactions");

        _transactionCounter = meter.CreateCounter<int>(
            "transactions_processed_total",
            "Number of transactions processed");

        _transactionDuration = meter.CreateHistogram<double>(
            "transaction_duration_seconds",
            "Duration of transaction processing");
    }

    public async Task<Result<Transaction>> ProcessTransactionAsync(CreateTransactionCommand command)
    {
        using var activity = Activity.StartActivity("ProcessTransaction");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await DoProcessTransactionAsync(command);

            _transactionCounter.Add(1, new TagList
            {
                ["type"] = command.Type.ToString(),
                ["status"] = result.IsSuccess ? "success" : "failure"
            });

            return result;
        }
        finally
        {
            stopwatch.Stop();
            _transactionDuration.Record(stopwatch.Elapsed.TotalSeconds);
        }
    }
}
```

### Profiling and Diagnostics

```csharp
// ✅ Good: Add diagnostic logging for performance issues
public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000) // Log slow requests
            {
                _logger.LogWarning("Slow request: {Method} {Path} took {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
```

## Performance Best Practices Summary

### Memory Management

1. **Use StringBuilder** for multiple string concatenations
2. **Choose appropriate collection types** and initialize with capacity when known
3. **Use object pooling** for expensive objects
4. **Implement proper disposal** patterns for resources

### Asynchronous Programming

1. **Use cancellation tokens** for long-running operations
2. **Apply ConfigureAwait(false)** in library code
3. **Limit parallelism** to prevent resource exhaustion
4. **Use proper async patterns** throughout the application

### Database Performance

1. **Use AsNoTracking** for read-only queries
2. **Select only needed columns** using projections
3. **Batch database operations** when possible
4. **Optimize connection pooling** settings

### Caching

1. **Cache frequently accessed data** with appropriate expiration
2. **Use distributed caching** for scalability
3. **Implement cache-aside pattern** for data consistency
4. **Monitor cache hit rates** and adjust strategies

### HTTP Performance

1. **Use HttpClientFactory** for connection pooling
2. **Enable response compression** for large payloads
3. **Configure appropriate timeouts** for external calls
4. **Implement retry policies** with exponential backoff

### Monitoring

1. **Add custom metrics** for business operations
2. **Log performance issues** and slow operations
3. **Use distributed tracing** for complex workflows
4. **Set up alerts** for performance degradation

Remember: Always measure performance before and after optimizations. Premature optimization can lead to complex code without real benefits. Focus on the bottlenecks that matter most to your application's performance profile.
