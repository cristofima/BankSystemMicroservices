# ASP.NET Core Best Practices

## Overview

This document provides comprehensive ASP.NET Core best practices for the Bank System Microservices project, focusing on performance, security, and maintainability.

## Performance Best Practices

### Avoid Blocking Calls

ASP.NET Core apps should process many requests simultaneously. Always use async/await patterns:

```csharp
// ✅ Good: Asynchronous operations
[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetTransactionByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}

// ❌ Bad: Blocking async calls
public class BadTransactionController : ControllerBase
{
    [HttpPost]
    public ActionResult<TransactionDto> CreateTransaction(CreateTransactionCommand command)
    {
        // This blocks a thread pool thread!
        var result = _mediator.Send(command).Result;
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<TransactionDto> GetTransaction(Guid id)
    {
        // This can cause deadlocks!
        var query = new GetTransactionByIdQuery(id);
        var result = _mediator.Send(query).GetAwaiter().GetResult();

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
```

### Return Large Collections with Pagination

Don't return large collections all at once. Implement pagination:

```csharp
// ✅ Good: Paginated results
[HttpGet]
public async Task<ActionResult<PagedResult<TransactionDto>>> GetTransactions(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    [FromQuery] Guid? accountId = null,
    CancellationToken cancellationToken = default)
{
    // Limit maximum page size to prevent abuse
    pageSize = Math.Min(pageSize, 100);

    var query = new GetTransactionsQuery(page, pageSize, accountId);
    var result = await _mediator.Send(query, cancellationToken);

    return Ok(result);
}

// ✅ Good: Use IAsyncEnumerable for streaming large datasets
[HttpGet("stream")]
public async IAsyncEnumerable<TransactionDto> GetTransactionsStream(
    [FromQuery] Guid accountId,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var transaction in _repository.GetTransactionsStreamAsync(accountId, cancellationToken))
    {
        yield return _mapper.Map<TransactionDto>(transaction);
    }
}

// ❌ Bad: Returning all data without pagination
[HttpGet]
public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAllTransactions()
{
    // This could return millions of records!
    var transactions = await _repository.GetAllAsync();
    return Ok(transactions);
}
```

### Optimize Data Access and I/O

Make all data access operations asynchronous and efficient:

```csharp
// ✅ Good: Optimized data access with caching
public class AccountService
{
    private readonly IAccountRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AccountService> _logger;

    public async Task<Account> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        // Try cache first
        var cacheKey = $"account_{accountId}";
        if (_cache.TryGetValue(cacheKey, out Account cachedAccount))
        {
            _logger.LogDebug("Account {AccountId} retrieved from cache", accountId);
            return cachedAccount;
        }

        // Use no-tracking query for read-only data
        var account = await _repository.GetByIdNoTrackingAsync(accountId, cancellationToken);

        if (account != null)
        {
            // Cache for 5 minutes
            _cache.Set(cacheKey, account, TimeSpan.FromMinutes(5));
        }

        return account;
    }
}

// ✅ Good: Minimize network round trips with projection
public async Task<AccountSummaryDto> GetAccountSummaryAsync(
    Guid accountId,
    CancellationToken cancellationToken)
{
    // Single query with projection to reduce data transfer
    return await _context.Accounts
        .Where(a => a.Id == accountId)
        .Select(a => new AccountSummaryDto
        {
            Id = a.Id,
            AccountNumber = a.AccountNumber,
            Balance = a.Balance,
            LastTransactionDate = a.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => t.CreatedAt)
                .FirstOrDefault()
        })
        .FirstOrDefaultAsync(cancellationToken);
}

// ❌ Bad: Multiple queries and full entity loading
public async Task<AccountSummaryDto> GetAccountSummaryBad(Guid accountId)
{
    // Multiple database round trips
    var account = await _context.Accounts.FindAsync(accountId); // Full entity
    var lastTransaction = await _context.Transactions
        .Where(t => t.AccountId == accountId)
        .OrderByDescending(t => t.CreatedAt)
        .FirstOrDefaultAsync(); // Separate query

    return new AccountSummaryDto
    {
        Id = account.Id,
        AccountNumber = account.AccountNumber,
        Balance = account.Balance,
        LastTransactionDate = lastTransaction?.CreatedAt
    };
}
```

### Use HttpClientFactory for HTTP Connections

Pool HTTP connections properly to avoid socket exhaustion:

```csharp
// ✅ Good: Use HttpClientFactory with typed clients
public class ExternalPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalPaymentService> _logger;

    public ExternalPaymentService(HttpClient httpClient, ILogger<ExternalPaymentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/payments", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PaymentResult>(cancellationToken);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to process payment for request {RequestId}", request.Id);
            throw;
        }
    }
}

// Register in Program.cs
builder.Services.AddHttpClient<ExternalPaymentService>(client =>
{
    client.BaseAddress = new Uri("https://api.paymentprovider.com");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "BankSystem/1.0");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// ❌ Bad: Creating HttpClient instances manually
public class BadPaymentService
{
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // Creates new HttpClient for each request - socket exhaustion!
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.paymentprovider.com");

        var response = await client.PostAsJsonAsync("/payments", request);
        return await response.Content.ReadFromJsonAsync<PaymentResult>();
    }
}
```

### Avoid Large Object Allocations

Minimize allocations in hot code paths:

```csharp
// ✅ Good: Use ArrayPool for large arrays
public class CsvReportGenerator
{
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    public async Task<byte[]> GenerateTransactionReportAsync(
        IEnumerable<Transaction> transactions,
        CancellationToken cancellationToken)
    {
        const int bufferSize = 1024 * 1024; // 1MB
        var buffer = _arrayPool.Rent(bufferSize);

        try
        {
            using var stream = new MemoryStream(buffer);
            using var writer = new StreamWriter(stream);

            await writer.WriteLineAsync("Date,Amount,Type,Description");

            foreach (var transaction in transactions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await writer.WriteLineAsync(
                    $"{transaction.Date:yyyy-MM-dd},{transaction.Amount},{transaction.Type},{transaction.Description}");
            }

            await writer.FlushAsync();
            return stream.ToArray();
        }
        finally
        {
            _arrayPool.Return(buffer);
        }
    }
}

// ✅ Good: Use StringBuilder for string concatenation
public string FormatTransactionSummary(IEnumerable<Transaction> transactions)
{
    var sb = new StringBuilder();
    sb.AppendLine("Transaction Summary");
    sb.AppendLine("==================");

    foreach (var transaction in transactions)
    {
        sb.AppendLine($"{transaction.Date:yyyy-MM-dd}: {transaction.Amount:C} - {transaction.Description}");
    }

    return sb.ToString();
}

// ❌ Bad: String concatenation in loops
public string FormatTransactionSummaryBad(IEnumerable<Transaction> transactions)
{
    var summary = "Transaction Summary\n";
    summary += "==================\n";

    foreach (var transaction in transactions)
    {
        // Creates new string objects in each iteration
        summary += $"{transaction.Date:yyyy-MM-dd}: {transaction.Amount:C} - {transaction.Description}\n";
    }

    return summary;
}
```

## Security Best Practices

### Handle HttpContext Properly

Never store HttpContext in fields or access it from multiple threads:

```csharp
// ✅ Good: Extract needed data from HttpContext
[ApiController]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    [HttpPost]
    public async Task<ActionResult> ProcessTransactionAsync(
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        // Extract needed data explicitly
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        // Pass explicit parameters instead of HttpContext
        var result = await _transactionService.ProcessTransactionAsync(
            command, userId, ipAddress, userAgent, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

// ❌ Bad: Storing HttpContext reference
public class BadService
{
    private readonly HttpContext _context; // Never do this!

    public BadService(IHttpContextAccessor accessor)
    {
        _context = accessor.HttpContext; // HttpContext is not thread-safe
    }

    public async Task DoWorkAsync()
    {
        // This could fail if HttpContext is accessed from different thread
        var userId = _context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
```

### Use Strongly-Typed Response Headers (ASP0015)

Always use strongly-typed properties for HTTP response headers:

```csharp
// ✅ Good: Use strongly-typed header properties
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Set security headers after response starts
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Use strongly-typed properties
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers.XXSSProtection = "1; mode=block";
            headers.ReferrerPolicy = "strict-origin-when-cross-origin";
            headers.ContentSecurityPolicy = "default-src 'self'";

            // For headers without strongly-typed properties, use string access
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

// ❌ Bad: String-based header access when strongly-typed properties exist
public class BadSecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Don't use string access when strongly-typed properties exist
        headers["X-Content-Type-Options"] = "nosniff"; // Use headers.XContentTypeOptions instead
        headers["X-Frame-Options"] = "DENY";          // Use headers.XFrameOptions instead
        headers["X-XSS-Protection"] = "1; mode=block"; // Use headers.XXSSProtection instead

        await _next(context);
    }
}
```

### Common Strongly-Typed Header Properties

```csharp
// Reference for available strongly-typed header properties
public static class HeaderExamples
{
    public static void SetCommonHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Security headers
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers.XXSSProtection = "1; mode=block";
        headers.ReferrerPolicy = "strict-origin-when-cross-origin";
        headers.ContentSecurityPolicy = "default-src 'self'";

        // Caching headers
        headers.CacheControl = "no-cache, no-store, must-revalidate";
        headers.Pragma = "no-cache";
        headers.Expires = "0";
        headers.ETag = "\"version-123\"";
        headers.LastModified = DateTimeOffset.UtcNow.ToString("R");

        // Content headers
        headers.ContentType = "application/json";
        headers.ContentLength = 1024;
        headers.ContentEncoding = "gzip";
        headers.ContentDisposition = "attachment; filename=data.json";

        // CORS headers
        headers.AccessControlAllowOrigin = "*";
        headers.AccessControlAllowMethods = "GET, POST, PUT, DELETE";
        headers.AccessControlAllowHeaders = "Content-Type, Authorization";

        // Custom headers (when no strongly-typed property exists)
        headers["Custom-Header"] = "custom-value";
        headers["Permissions-Policy"] = "camera=(), microphone=()";
    }
}
```

### Implement Proper Error Handling

Don't expose internal details in error responses:

```csharp
// ✅ Good: Secure error handling middleware
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. Path: {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Type = "validation_error",
                Title = "Validation Failed",
                Status = 400,
                Detail = "One or more validation errors occurred",
                Errors = validationEx.Errors
            },
            DomainException domainEx => new ErrorResponse
            {
                Type = "business_error",
                Title = "Business Rule Violation",
                Status = 400,
                Detail = domainEx.Message
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                Type = "unauthorized",
                Title = "Unauthorized",
                Status = 401,
                Detail = "Authentication is required"
            },
            _ => new ErrorResponse
            {
                Type = "internal_error",
                Title = "Internal Server Error",
                Status = 500,
                Detail = _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred"
            }
        };

        context.Response.StatusCode = response.Status;
        context.Response.Headers.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public record ErrorResponse
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public object? Errors { get; init; }
}
```

## Background Processing

### Use Background Services for Long-Running Tasks

Don't block HTTP requests with long-running operations:

```csharp
// ✅ Good: Background service for processing queued work
public class TransactionProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionProcessingService> _logger;

    public TransactionProcessingService(
        IServiceProvider serviceProvider,
        ILogger<TransactionProcessingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transaction processing service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var transactionQueue = scope.ServiceProvider.GetRequiredService<ITransactionQueue>();

                var pendingTransactions = await transactionQueue.DequeueBatchAsync(10, stoppingToken);

                if (pendingTransactions.Any())
                {
                    var tasks = pendingTransactions.Select(ProcessTransactionAsync);
                    await Task.WhenAll(tasks);

                    _logger.LogInformation("Processed {Count} transactions", pendingTransactions.Count());
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction processing service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Back off on error
            }
        }
    }

    private async Task ProcessTransactionAsync(QueuedTransaction queuedTransaction)
    {
        using var scope = _serviceProvider.CreateScope();
        var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();

        try
        {
            await transactionService.ProcessAsync(queuedTransaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transaction {TransactionId}", queuedTransaction.Id);
        }
    }
}

// ✅ Good: Controller queues work and returns immediately
[ApiController]
[Route("api/[controller]")]
public class BulkTransactionController : ControllerBase
{
    private readonly ITransactionQueue _transactionQueue;

    [HttpPost("bulk")]
    public async Task<ActionResult<BulkJobResponse>> QueueBulkTransactions(
        [FromBody] IEnumerable<CreateTransactionCommand> commands,
        CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();

        await _transactionQueue.EnqueueBulkAsync(jobId, commands, cancellationToken);

        return Accepted(new BulkJobResponse
        {
            JobId = jobId,
            Status = "Queued",
            EstimatedCompletionTime = DateTime.UtcNow.AddMinutes(5)
        });
    }

    [HttpGet("bulk/{jobId:guid}/status")]
    public async Task<ActionResult<BulkJobStatus>> GetBulkJobStatus(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var status = await _transactionQueue.GetJobStatusAsync(jobId, cancellationToken);
        return Ok(status);
    }
}

// ❌ Bad: Processing in controller action
[HttpPost("bulk")]
public async Task<ActionResult> ProcessBulkTransactionsBad(
    [FromBody] IEnumerable<CreateTransactionCommand> commands)
{
    // This blocks the HTTP request until all transactions are processed
    foreach (var command in commands)
    {
        await _transactionService.ProcessTransactionAsync(command);
    }

    return Ok("All transactions processed"); // User waits for everything to complete
}
```

## Model Validation

### Use Data Annotations and FluentValidation

```csharp
// ✅ Good: Comprehensive validation
public record CreateAccountRequest
{
    [Required(ErrorMessage = "Customer ID is required")]
    public Guid CustomerId { get; init; }

    [Required(ErrorMessage = "Account type is required")]
    [EnumDataType(typeof(AccountType), ErrorMessage = "Invalid account type")]
    public AccountType AccountType { get; init; }

    [Range(0.01, 1000000, ErrorMessage = "Initial deposit must be between $0.01 and $1,000,000")]
    public decimal InitialDeposit { get; init; }

    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be 3 characters")]
    public string Currency { get; init; } = string.Empty;
}

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.AccountType)
            .IsInEnum()
            .WithMessage("Invalid account type");

        RuleFor(x => x.InitialDeposit)
            .GreaterThan(0)
            .WithMessage("Initial deposit must be positive")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Initial deposit cannot exceed $1,000,000");

        RuleFor(x => x.Currency)
            .Must(BeValidCurrency)
            .WithMessage("Invalid currency code");
    }

    private bool BeValidCurrency(string currency)
    {
        return !string.IsNullOrEmpty(currency) &&
               currency.Length == 3 &&
               IsoCurrencyCodes.Contains(currency.ToUpperInvariant());
    }
}
```

## Summary

1. **Always use async/await** for I/O operations
2. **Implement pagination** for large data sets
3. **Use HttpClientFactory** for external HTTP calls
4. **Minimize large object allocations** in hot code paths
5. **Handle HttpContext properly** without storing references
6. **Use strongly-typed headers** instead of string-based access
7. **Implement secure error handling** that doesn't expose internal details
8. **Use background services** for long-running operations
9. **Validate input comprehensively** using multiple validation layers
10. **Cache appropriately** to improve performance
