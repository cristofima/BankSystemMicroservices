# Error Handling Guidelines

## Overview

Proper error handling is crucial for building robust and maintainable applications. This guideline provides patterns and practices for error handling in the Bank System Microservices project.

## Result Pattern

The Result pattern is a functional approach to error handling that avoids exceptions for business logic errors.

### Basic Result Implementation

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public string Error { get; }

    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static Result<T> Failure(string error) => new(false, default, error);

    // Extension methods for better usability
    public Result<TNew> Map<TNew>(Func<T, TNew> func)
    {
        return IsSuccess
            ? Result<TNew>.Success(func(Value))
            : Result<TNew>.Failure(Error);
    }

    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> func)
    {
        if (IsFailure)
            return Result<TNew>.Failure(Error);

        var result = await func(Value);
        return Result<TNew>.Success(result);
    }

    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> func)
    {
        return IsSuccess ? func(Value) : Result<TNew>.Failure(Error);
    }
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);

    public Result<T> Map<T>(Func<T> func)
    {
        return IsSuccess
            ? Result<T>.Success(func())
            : Result<T>.Failure(Error);
    }

    public Result Bind(Func<Result> func)
    {
        return IsSuccess ? func() : this;
    }
}
```

### Advanced Result with Multiple Errors

```csharp
public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    private ValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static ValidationResult Success() => new(true, Array.Empty<string>());

    public static ValidationResult Failure(string error) => new(false, new[] { error });

    public static ValidationResult Failure(IEnumerable<string> errors) =>
        new(false, errors.ToList().AsReadOnly());

    public ValidationResult Combine(ValidationResult other)
    {
        if (IsValid && other.IsValid)
            return Success();

        var allErrors = Errors.Concat(other.Errors).ToList();
        return new ValidationResult(false, allErrors.AsReadOnly());
    }
}
```

## Exception Handling

### Custom Domain Exceptions

Create specific exceptions for different domain errors:

```csharp
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class AccountNotFoundException : DomainException
{
    public Guid AccountId { get; }

    public AccountNotFoundException(Guid accountId)
        : base($"Account with ID '{accountId}' was not found")
    {
        AccountId = accountId;
    }
}

public class InsufficientFundsException : DomainException
{
    public decimal RequestedAmount { get; }
    public decimal AvailableBalance { get; }

    public InsufficientFundsException(decimal requestedAmount, decimal availableBalance)
        : base($"Insufficient funds. Requested: {requestedAmount:C}, Available: {availableBalance:C}")
    {
        RequestedAmount = requestedAmount;
        AvailableBalance = availableBalance;
    }
}

public class InvalidTransactionException : DomainException
{
    public string TransactionId { get; }
    public string Reason { get; }

    public InvalidTransactionException(string transactionId, string reason)
        : base($"Invalid transaction {transactionId}: {reason}")
    {
        TransactionId = transactionId;
        Reason = reason;
    }
}

public class AccountLockedException : DomainException
{
    public Guid AccountId { get; }
    public DateTime LockedUntil { get; }

    public AccountLockedException(Guid accountId, DateTime lockedUntil)
        : base($"Account {accountId} is locked until {lockedUntil:yyyy-MM-dd HH:mm:ss}")
    {
        AccountId = accountId;
        LockedUntil = lockedUntil;
    }
}
```

### Infrastructure Exceptions

```csharp
public abstract class InfrastructureException : Exception
{
    protected InfrastructureException(string message) : base(message) { }
    protected InfrastructureException(string message, Exception innerException) : base(message, innerException) { }
}

public class DatabaseConnectionException : InfrastructureException
{
    public DatabaseConnectionException(string message, Exception innerException)
        : base($"Database connection failed: {message}", innerException) { }
}

public class ExternalServiceException : InfrastructureException
{
    public string ServiceName { get; }
    public int? StatusCode { get; }

    public ExternalServiceException(string serviceName, string message, int? statusCode = null)
        : base($"External service '{serviceName}' failed: {message}")
    {
        ServiceName = serviceName;
        StatusCode = statusCode;
    }
}
```

## Global Exception Handling

### Exception Middleware

```csharp
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (400, new ErrorResponse
            {
                Type = "validation_error",
                Title = "Validation Failed",
                Status = 400,
                Detail = "One or more validation errors occurred",
                Errors = validationEx.Errors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToArray())
            }),

            AccountNotFoundException => (404, new ErrorResponse
            {
                Type = "not_found",
                Title = "Resource Not Found",
                Status = 404,
                Detail = exception.Message
            }),

            InsufficientFundsException => (400, new ErrorResponse
            {
                Type = "insufficient_funds",
                Title = "Insufficient Funds",
                Status = 400,
                Detail = exception.Message
            }),

            DomainException => (400, new ErrorResponse
            {
                Type = "domain_error",
                Title = "Business Rule Violation",
                Status = 400,
                Detail = exception.Message
            }),

            UnauthorizedAccessException => (401, new ErrorResponse
            {
                Type = "unauthorized",
                Title = "Unauthorized",
                Status = 401,
                Detail = "Authentication is required"
            }),

            _ => (500, new ErrorResponse
            {
                Type = "internal_error",
                Title = "Internal Server Error",
                Status = 500,
                Detail = "An unexpected error occurred"
            })
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? Instance { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

## Validation Error Handling

### FluentValidation Integration

```csharp
public class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Domain Validation

```csharp
public static class Guard
{
    public static void AgainstNull<T>(T value, string parameterName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(parameterName);
    }

    public static void AgainstNullOrEmpty(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty", parameterName);
    }

    public static void AgainstNegative(decimal value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentException($"Value cannot be negative: {value}", parameterName);
    }

    public static void AgainstZeroOrNegative(decimal value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentException($"Value must be positive: {value}", parameterName);
    }

    public static void AgainstInvalidRange(decimal value, decimal min, decimal max, string parameterName)
    {
        if (value < min || value > max)
            throw new ArgumentException($"Value {value} is not within range [{min}, {max}]", parameterName);
    }
}
```

## Logging Best Practices

### Structured Logging

```csharp
public class TransactionService
{
    private readonly ILogger<TransactionService> _logger;

    public async Task<Result<TransactionDto>> ProcessTransactionAsync(
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = Guid.NewGuid(),
            ["AccountId"] = command.AccountId,
            ["TransactionType"] = command.Type.ToString()
        });

        try
        {
            _logger.LogInformation("Starting transaction processing for account {AccountId} with amount {Amount}",
                command.AccountId, command.Amount);

            var account = await _accountRepository.GetByIdAsync(command.AccountId, cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Account {AccountId} not found for transaction processing", command.AccountId);
                return Result<TransactionDto>.Failure("Account not found");
            }

            // Process transaction...

            _logger.LogInformation("Transaction processed successfully for account {AccountId}. Transaction ID: {TransactionId}",
                command.AccountId, result.Value.Id);

            return result;
        }
        catch (InsufficientFundsException ex)
        {
            _logger.LogWarning(ex, "Insufficient funds for transaction on account {AccountId}. Requested: {RequestedAmount}, Available: {AvailableBalance}",
                command.AccountId, ex.RequestedAmount, ex.AvailableBalance);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing transaction for account {AccountId}", command.AccountId);
            throw;
        }
    }
}
```

## Testing Error Scenarios

### Unit Testing Exceptions

```csharp
public class AccountTests
{
    [Fact]
    public void Withdraw_InsufficientFunds_ShouldReturnFailure()
    {
        // Arrange
        var account = Account.CreateNew("123456789", Guid.NewGuid(), new Money(100, Currency.USD));
        var withdrawAmount = new Money(200, Currency.USD);

        // Act
        var result = account.Withdraw(withdrawAmount, "Test withdrawal");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Insufficient funds", result.Error);
    }

    [Fact]
    public void CreateAccount_NegativeInitialDeposit_ShouldThrowException()
    {
        // Arrange, Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Money(-100, Currency.USD));

        // Optionally assert exception message
        // Assert.Contains("some expected message", ex.Message);
    }
}
```

### Integration Testing Error Handling

```csharp
public class TransactionControllerTests
{
    private readonly HttpClient _httpClient;

    public TransactionControllerTests()
    {
        // Assume _httpClient is set up via TestServer or factory
        _httpClient = TestServerFactory.CreateClient(); // Replace with your setup
    }

    [Fact]
    public async Task CreateTransaction_AccountNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentAccountId = Guid.NewGuid();
        var command = new CreateTransactionCommand
        {
            AccountId = nonExistentAccountId,
            Amount = 100
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/transactions", command);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content);

        Assert.Equal("not_found", errorResponse.Type);
    }
}
```

## Best Practices Summary

1. **Use Result Pattern**: For business logic errors, prefer Result pattern over exceptions
2. **Specific Exceptions**: Create specific exception types for different error scenarios
3. **Global Exception Handling**: Implement centralized exception handling middleware
4. **Structured Logging**: Use structured logging with correlation IDs
5. **Don't Swallow Exceptions**: Always log exceptions before handling them
6. **Fail Fast**: Validate inputs early and fail fast
7. **Meaningful Error Messages**: Provide clear, actionable error messages
8. **Security Considerations**: Don't expose sensitive information in error messages
9. **Test Error Scenarios**: Write tests for both success and failure cases
10. **Document Error Codes**: Maintain documentation of error codes and their meanings

---

_This guideline follows the principles outlined in [Clean Code Guidelines](./clean-code.md) and [SOLID Principles](./solid-principles.md)._
