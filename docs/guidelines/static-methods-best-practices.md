# Static Methods and CA1822 Best Practices

## Overview

This document provides comprehensive guidelines for using static methods in .NET applications, addressing the CA1822 analyzer rule, and implementing best practices for method design in the Bank System Microservices project.

## Understanding CA1822 Analyzer Rule

The CA1822 analyzer rule flags methods that don't access instance data and suggests marking them as static. This improves performance and makes the code intent clearer.

**Why static methods matter:**

- Better performance (no `this` parameter)
- Clear intent that method doesn't depend on instance state
- Can be called without creating an instance
- Memory efficient for utility functions

## When to Use Static Methods

### Utility Functions and Helpers

```csharp
// ✅ Good: Static methods for utility functions
public class TokenResponseFactory
{
    // Static method - doesn't need instance data
    public static TokenResponse CreateTokenResponse(string accessToken, string refreshToken, int expiresIn)
    {
        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            TokenType = "Bearer"
        };
    }

    // Static method for validation
    public static bool IsValidTokenFormat(string token)
    {
        return !string.IsNullOrEmpty(token) && token.Length >= 32;
    }
}
```

### Factory Methods in Domain Entities

```csharp
// ✅ Good: Static factory methods in domain entities
public class Account
{
    private readonly List<Transaction> _transactions = new();

    // Instance method - uses instance data
    public Result Withdraw(Money amount, string description)
    {
        // Uses instance fields
        if (Balance.Amount < amount.Amount)
            return Result.Failure("Insufficient funds");

        // Implementation...
    }

    // Static factory method - creates new instances
    public static Account CreateNew(string accountNumber, Guid customerId, Money initialDeposit)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            CustomerId = customerId,
            Balance = initialDeposit,
            Status = AccountStatus.Active
        };
    }

    // Static validation method
    public static bool IsValidAccountNumber(string accountNumber)
    {
        return !string.IsNullOrEmpty(accountNumber) &&
               accountNumber.Length == 10 &&
               accountNumber.All(char.IsDigit);
    }
}
```

## Response and DTO Factory Patterns

### Static Factory Methods for DTOs

```csharp
// ✅ Good: Static factory methods for DTOs
public static class ResponseFactory
{
    public static ApiResponse<T> Success<T>(T data, string message = "Operation successful")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Failure<T>(string error, IEnumerable<string>? additionalErrors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = error,
            Errors = additionalErrors ?? Enumerable.Empty<string>(),
            Timestamp = DateTime.UtcNow
        };
    }

    public static PagedResult<T> CreatePagedResult<T>(
        IEnumerable<T> data,
        int currentPage,
        int pageSize,
        int totalRecords)
    {
        return new PagedResult<T>
        {
            Data = data,
            Pagination = new PaginationInfo
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                HasNextPage = currentPage * pageSize < totalRecords,
                HasPreviousPage = currentPage > 1
            }
        };
    }
}
```

## Validation Helper Methods

```csharp
// ✅ Good: Static validation helpers
public static class ValidationHelpers
{
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidCurrency(string currencyCode)
    {
        return !string.IsNullOrEmpty(currencyCode) &&
               currencyCode.Length == 3 &&
               CurrencyCodes.IsValid(currencyCode);
    }

    public static bool IsValidAmount(decimal amount)
    {
        return amount > 0 && amount <= 1_000_000m;
    }

    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove common formatting characters
        var cleaned = Regex.Replace(phoneNumber, @"[\s\-\(\)\+]", "");

        // Check if it's all digits and reasonable length
        return Regex.IsMatch(cleaned, @"^\d{10,15}$");
    }
}
```

## Mapping and Conversion Utilities

```csharp
// ✅ Good: Static mapping methods
public static class EntityMappers
{
    public static UserResponse ToUserResponse(this ApplicationUser user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public static TransactionDto ToTransactionDto(this Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        };
    }

    // Static conversion methods
    public static decimal ToDecimal(this Money money)
    {
        return money.Amount;
    }

    public static Money ToMoney(this decimal amount, string currencyCode = "USD")
    {
        return new Money(amount, new Currency(currencyCode));
    }

    public static string ToFormattedString(this Money money)
    {
        return $"{money.Amount:C} {money.Currency.Code}";
    }
}
```

## Constants and Configuration Helpers

```csharp
// ✅ Good: Static constants and helpers
public static class SecurityConstants
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 100;
    public const int DefaultTokenExpiryMinutes = 60;
    public const int MaxFailedLoginAttempts = 5;

    public static TimeSpan GetLockoutDuration() => TimeSpan.FromMinutes(15);

    public static bool IsPasswordCompliant(string password)
    {
        return !string.IsNullOrEmpty(password) &&
               password.Length >= MinPasswordLength &&
               password.Length <= MaxPasswordLength &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => !char.IsLetterOrDigit(c));
    }

    public static string GenerateSecurePassword(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

public static class BusinessConstants
{
    public const decimal MaxDailyTransactionLimit = 50000m;
    public const decimal MaxSingleTransactionAmount = 25000m;
    public const int MaxTransactionsPerDay = 100;
    public const int AccountNumberLength = 10;

    public static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP", "CAD" };

    public static bool IsSupportedCurrency(string currencyCode)
    {
        return SupportedCurrencies.Contains(currencyCode, StringComparer.OrdinalIgnoreCase);
    }

    public static decimal CalculateFee(decimal amount, string transactionType)
    {
        return transactionType.ToLower() switch
        {
            "international" => amount * 0.025m,
            "express" => Math.Max(amount * 0.01m, 5.00m),
            _ => 0m
        };
    }
}
```

## When NOT to Use Static Methods

### Avoid Static Methods for Services with Dependencies

```csharp
// ❌ Bad: Don't make methods static if they might need instance data later
public class EmailService
{
    private readonly IEmailConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    // Don't make this static - it uses injected dependencies
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Sending email to {Email}", to);
        // Uses _config for SMTP settings
        // Implementation...
    }
}

// ❌ Bad: Don't make repository methods static
public class UserRepository
{
    private readonly DbContext _context;

    // Don't make this static - it uses DbContext
    public async Task<User> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }
}
```

### Avoid Static Methods for Testability

```csharp
// ❌ Bad: Static methods are harder to mock
public static class PaymentProcessor
{
    public static async Task<bool> ProcessPaymentAsync(decimal amount)
    {
        // Hard to mock in tests
        var httpClient = new HttpClient();
        // Implementation...
    }
}

// ✅ Good: Use dependency injection for better testability
public class PaymentProcessor
{
    private readonly HttpClient _httpClient;

    public PaymentProcessor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ProcessPaymentAsync(decimal amount)
    {
        // Can be easily mocked in tests
        // Implementation...
    }
}
```

## Addressing CA1822 in Code Reviews

When the CA1822 analyzer flags a method, consider these factors:

### Evaluation Criteria

1. **Does the method use any instance fields or properties?**

   - If no, consider making it static
   - If yes, keep as instance method

2. **Will it likely need instance data in the future?**

   - If yes, consider suppressing the warning
   - If no, make it static

3. **Is it a utility/helper method?**
   - If yes, make it static
   - If no, evaluate based on business logic

### Suppression When Appropriate

```csharp
// Suppress CA1822 if method might need instance data in future
[SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "Method may require instance data in future iterations")]
public string GetDeviceInfo()
{
    // Method implementation that doesn't currently use instance data
    // but may need to access configuration or other dependencies later
    return "Device info";
}

// Or use a more descriptive justification
[SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "Method will access injected services in upcoming feature")]
public ValidationResult ValidateBusinessRule()
{
    // Current implementation doesn't use instance data
    // but will access _businessRuleService in next sprint
    return ValidationResult.Success();
}
```

## Best Practices Summary

### Use Static Methods For

1. **Factory methods** that create new instances
2. **Validation and utility functions** without side effects
3. **Pure functions** that don't depend on external state
4. **Extension methods** and mapping functions
5. **Constants and configuration helpers**
6. **Mathematical calculations** and transformations

### Avoid Static Methods For

1. **Methods that use dependency injection**
2. **Repository and service methods** that access external resources
3. **Methods that might need instance data later**
4. **Controller actions** (except in rare cases)
5. **Methods that need to be mocked** in unit tests

### Consider Testability

- **Static methods are harder to mock** in unit tests
- **Use dependency injection** for better testability
- **Keep static methods pure and simple** without external dependencies
- **Prefer instance methods** when method might evolve to use injected services

### Performance Considerations

- **Static methods have slight performance benefits** (no `this` parameter)
- **Memory efficient** for frequently called utility functions
- **Consider static constructors** for expensive initialization
- **Use static readonly** for immutable reference types

## Common Patterns

### Static Builder Pattern

```csharp
public static class QueryBuilder
{
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int page, int pageSize)
    {
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    public static IQueryable<Transaction> FilterByDateRange(
        this IQueryable<Transaction> query,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        return query;
    }
}
```

### Static Cache Helpers

```csharp
public static class CacheKeys
{
    public static string UserProfile(string userId) => $"user_profile_{userId}";
    public static string AccountBalance(Guid accountId) => $"account_balance_{accountId}";
    public static string DailyTransactions(Guid accountId, DateTime date)
        => $"daily_transactions_{accountId}_{date:yyyyMMdd}";

    public static TimeSpan DefaultExpiry => TimeSpan.FromMinutes(15);
    public static TimeSpan ShortExpiry => TimeSpan.FromMinutes(5);
    public static TimeSpan LongExpiry => TimeSpan.FromHours(1);
}
```

Remember: The goal is to write clear, maintainable, and testable code. Use static methods when they improve code organization and performance, but don't sacrifice testability or future extensibility for minor performance gains.
