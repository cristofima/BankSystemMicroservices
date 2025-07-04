# Code Review Guidelines

## Overview

This document provides comprehensive guidelines for conducting effective code reviews in the Bank System Microservices project. These guidelines ensure code quality, maintainability, security, and adherence to best practices.

**Important**: All code reviews should verify adherence to [Clean Code principles](./clean-code.md). Use the Clean Code checklist as the foundation for all reviews.

## Code Review Process

### Pre-Review Checklist

Before submitting code for review, ensure:

```csharp
// ✅ Self-review completed
// ✅ All tests pass locally
// ✅ Follows Clean Code principles (see clean-code.md)
// ✅ Functions are small and focused (< 20 lines)
// ✅ Classes have single responsibility
// ✅ Names are intention-revealing and searchable
// ✅ No code smells present
// ✅ No hardcoded values or secrets
// ✅ Error handling implemented
// ✅ Logging added where appropriate
// ✅ Documentation updated
// ✅ Performance considerations addressed
```

### Clean Code Review Checklist

Use this checklist during every code review:

#### Meaningful Names

- [ ] Class names are nouns (Customer, TransactionProcessor)
- [ ] Method names are verbs (ProcessTransaction, ValidateAccount)
- [ ] Names reveal intent without needing comments
- [ ] No misleading or ambiguous names
- [ ] Constants are searchable, not magic numbers

#### Functions

- [ ] Functions are small (< 20 lines)
- [ ] Functions do one thing well
- [ ] Function arguments are minimal (0-3 parameters)
- [ ] No side effects beyond the function's stated purpose
- [ ] Command Query Separation is maintained

#### Classes

- [ ] Single Responsibility Principle followed
- [ ] High cohesion (methods work with same data)
- [ ] Low coupling between classes
- [ ] Open/Closed Principle considered

#### Comments

- [ ] Code is self-documenting
- [ ] Comments explain "why," not "what"
- [ ] No commented-out code
- [ ] No redundant or misleading comments

#### Error Handling

- [ ] Uses Result pattern for business errors
- [ ] Uses exceptions for system errors
- [ ] No null returns (use Result pattern instead)
- [ ] Proper exception context provided

### Review Categories

#### 1. Architecture & Design Review

**Clean Architecture Compliance**

```csharp
// ✅ Good: Proper layer separation
namespace BankSystem.Account.Application.Commands
{
    public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<AccountDto>>
    {
        private readonly IAccountRepository _repository; // Infrastructure dependency
        private readonly IMapper _mapper; // Application dependency
        private readonly ILogger<CreateAccountCommandHandler> _logger;

        // Handler only contains application logic
        public async Task<Result<AccountDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            // Domain logic delegation
            var account = Account.CreateNew(request.CustomerId, request.InitialDeposit);

            // Infrastructure operations
            await _repository.AddAsync(account, cancellationToken);

            return Result<AccountDto>.Success(_mapper.Map<AccountDto>(account));
        }
    }
}

// ❌ Bad: Mixing concerns and layer violations
namespace BankSystem.Account.Application.Commands
{
    public class CreateAccountCommandHandler
    {
        public async Task<AccountDto> Handle(CreateAccountCommand request)
        {
            // Direct database access in application layer
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Business logic mixed with data access
            var sql = "INSERT INTO Accounts...";
            await connection.ExecuteAsync(sql, request);

            // Direct email sending in command handler
            await _emailService.SendWelcomeEmailAsync(request.Email);
        }
    }
}
```

**SOLID Principles Verification**

```csharp
// ✅ Good: Single Responsibility
public class AccountValidator
{
    public ValidationResult ValidateAccount(Account account)
    {
        // Only validates accounts
    }
}

public class AccountNotificationService
{
    public async Task NotifyAccountCreatedAsync(Account account)
    {
        // Only handles notifications
    }
}

// ❌ Bad: Multiple responsibilities
public class AccountService
{
    public void CreateAccount(Account account) { }
    public void SendEmail(string email) { }
    public void LogTransaction(Transaction transaction) { }
    public void ValidateInput(object input) { }
}
```

#### 2. Domain Logic Review

**Rich Domain Models**

```csharp
// ✅ Good: Rich domain model with behavior
public class Account : AggregateRoot<Guid>
{
    private readonly List<Transaction> _transactions = new();

    public Money Balance { get; private set; }
    public AccountStatus Status { get; private set; }

    public Result Withdraw(Money amount, string description)
    {
        // Guard clauses
        if (Status != AccountStatus.Active)
            return Result.Failure("Account is not active");

        if (Balance.Amount < amount.Amount)
            return Result.Failure("Insufficient funds");

        // Business logic
        Balance = Balance.Subtract(amount);
        var transaction = Transaction.CreateWithdrawal(Id, amount, description);
        _transactions.Add(transaction);

        // Domain event
        AddDomainEvent(new MoneyWithdrawnEvent(Id, amount, Balance));

        return Result.Success();
    }
}

// ❌ Bad: Anemic domain model
public class Account
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; }
    // No behavior, just data
}

public class AccountService
{
    public void Withdraw(Account account, decimal amount)
    {
        // Business logic in service instead of domain
        if (account.Status != "Active") throw new Exception("...");
        if (account.Balance < amount) throw new Exception("...");
        account.Balance -= amount;
    }
}
```

**Value Objects Usage**

```csharp
// ✅ Good: Value objects for complex types
public record Money(decimal Amount, Currency Currency)
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} to {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    // Validation in constructor
    public Money
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
    }
}

// ❌ Bad: Primitive obsession
public class Account
{
    public decimal Balance { get; set; } // What currency? Validation?
    public string Currency { get; set; } // String prone to errors
}
```

#### 3. Error Handling Review

**Result Pattern Implementation**

```csharp
// ✅ Good: Consistent error handling with Result pattern
public async Task<Result<TransactionDto>> ProcessTransactionAsync(CreateTransactionCommand command)
{
    try
    {
        var account = await _repository.GetByIdAsync(command.AccountId);
        if (account == null)
            return Result<TransactionDto>.Failure("Account not found");

        var withdrawResult = account.Withdraw(new Money(command.Amount, Currency.USD), command.Description);
        if (!withdrawResult.IsSuccess)
            return Result<TransactionDto>.Failure(withdrawResult.Error);

        await _repository.UpdateAsync(account);
        return Result<TransactionDto>.Success(_mapper.Map<TransactionDto>(withdrawResult.Value));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing transaction for account {AccountId}", command.AccountId);
        return Result<TransactionDto>.Failure("An error occurred while processing the transaction");
    }
}

// ❌ Bad: Inconsistent error handling
public async Task<TransactionDto> ProcessTransactionAsync(CreateTransactionCommand command)
{
    var account = await _repository.GetByIdAsync(command.AccountId);
    if (account == null)
        throw new NotFoundException("Account not found"); // Exception for business logic

    account.Withdraw(command.Amount); // Might throw exception
    await _repository.UpdateAsync(account); // No error handling

    return _mapper.Map<TransactionDto>(account.LastTransaction); // Assumes success
}
```

#### 4. Security Review

**Input Validation**

```csharp
// ✅ Good: Comprehensive input validation
[HttpPost]
public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] CreateAccountRequest request)
{
    // Model validation is automatic with [ApiController]

    // Additional business validation
    if (!Currency.IsValidCode(request.Currency))
        return BadRequest("Invalid currency code");

    if (request.InitialDeposit < 0)
        return BadRequest("Initial deposit cannot be negative");

    var command = new CreateAccountCommand(request.CustomerId, request.InitialDeposit, request.Currency);
    var result = await _mediator.Send(command);

    return result.IsSuccess
        ? CreatedAtAction(nameof(GetAccount), new { accountId = result.Value.Id }, result.Value)
        : BadRequest(result.Error);
}

// ❌ Bad: Insufficient validation
[HttpPost]
public async Task<AccountDto> CreateAccount([FromBody] CreateAccountRequest request)
{
    // No validation - trusting client input
    var account = new Account
    {
        CustomerId = request.CustomerId,
        Balance = request.InitialDeposit // Could be negative
    };

    await _repository.AddAsync(account);
    return _mapper.Map<AccountDto>(account);
}
```

**Authentication and Authorization**

```csharp
// ✅ Good: Proper authorization checks
[HttpDelete("{accountId:guid}")]
[Authorize(Policy = "CanDeleteAccounts")]
public async Task<ActionResult> DeleteAccount(Guid accountId)
{
    // Additional authorization check
    var hasPermission = await _authorizationService.AuthorizeAsync(
        User, accountId, "CanDeleteSpecificAccount");

    if (!hasPermission.Succeeded)
        return Forbid();

    var command = new DeleteAccountCommand(accountId);
    var result = await _mediator.Send(command);

    return result.IsSuccess ? NoContent() : NotFound();
}

// ❌ Bad: Missing authorization
[HttpDelete("{accountId:guid}")]
public async Task<ActionResult> DeleteAccount(Guid accountId)
{
    // No authorization check - anyone can delete any account
    await _repository.DeleteAsync(accountId);
    return NoContent();
}
```

**Sensitive Data Handling**

```csharp
// ✅ Good: Proper sensitive data handling
public class CustomerDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;

    [JsonIgnore] // Never serialize SSN
    public string SocialSecurityNumber { get; init; } = string.Empty;

    // Masked for display
    public string MaskedSSN => SocialSecurityNumber.Length >= 4
        ? $"***-**-{SocialSecurityNumber[^4..]}"
        : "****";
}

// Logging without sensitive data
_logger.LogInformation("Customer {CustomerId} updated profile", customer.Id);

// ❌ Bad: Exposing sensitive data
public class CustomerDto
{
    public string SocialSecurityNumber { get; set; } // Exposed in API
    public string CreditCardNumber { get; set; } // Exposed in API
}

// Logging sensitive data
_logger.LogInformation("Customer {Customer} updated", JsonSerializer.Serialize(customer));
```

#### 5. Performance Review

**Async/Await Usage**

```csharp
// ✅ Good: Proper async implementation
public async Task<Result<IEnumerable<AccountDto>>> GetCustomerAccountsAsync(
    Guid customerId,
    CancellationToken cancellationToken = default)
{
    try
    {
        var accounts = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        var accountDtos = await Task.WhenAll(
            accounts.Select(async account =>
            {
                var balance = await _balanceService.GetCurrentBalanceAsync(account.Id, cancellationToken);
                return _mapper.Map<AccountDto>(account) with { CurrentBalance = balance };
            }));

        return Result<IEnumerable<AccountDto>>.Success(accountDtos);
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Operation cancelled for customer {CustomerId}", customerId);
        throw;
    }
}

// ❌ Bad: Blocking async calls
public IEnumerable<AccountDto> GetCustomerAccounts(Guid customerId)
{
    var accounts = _repository.GetByCustomerIdAsync(customerId).Result; // Deadlock risk
    return _mapper.Map<IEnumerable<AccountDto>>(accounts);
}
```

**Database Query Optimization**

```csharp
// ✅ Good: Optimized queries
public async Task<PagedResult<TransactionDto>> GetTransactionHistoryAsync(
    Guid accountId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    var query = _context.Transactions
        .Where(t => t.AccountId == accountId)
        .OrderByDescending(t => t.Timestamp)
        .Select(t => new TransactionDto // Project only needed fields
        {
            Id = t.Id,
            Amount = t.Amount,
            Description = t.Description,
            Timestamp = t.Timestamp
        });

    var totalCount = await query.CountAsync(cancellationToken);
    var transactions = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
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
    var transactions = await _context.Transactions
        .Include(t => t.Account) // Unnecessary include
        .Include(t => t.Account.Customer) // Even more unnecessary
        .ToListAsync(); // Loads all transactions

    return transactions
        .Where(t => t.AccountId == accountId) // Filtering in memory
        .Select(t => _mapper.Map<TransactionDto>(t))
        .ToList();
}
```

#### 6. Testing Review

**Unit Test Quality**

```csharp
// ✅ Good: Well-structured unit test
[TestFixture]
public class AccountTests
{
    private Account _account;

    [SetUp]
    public void Setup()
    {
        _account = Account.CreateNew("12345", Guid.NewGuid(), new Money(1000m, Currency.USD));
    }

    [Test]
    public void Withdraw_SufficientFunds_ShouldSucceed()
    {
        // Arrange
        var withdrawAmount = new Money(500m, Currency.USD);
        var description = "ATM Withdrawal";

        // Act
        var result = _account.Withdraw(withdrawAmount, description);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_account.Balance.Amount, Is.EqualTo(500m));
        Assert.That(_account.Transactions.Count, Is.EqualTo(1));
        Assert.That(_account.Transactions.First().Type, Is.EqualTo(TransactionType.Withdrawal));
    }

    [Test]
    public void Withdraw_InsufficientFunds_ShouldFail()
    {
        // Arrange
        var withdrawAmount = new Money(1500m, Currency.USD);
        var description = "ATM Withdrawal";

        // Act
        var result = _account.Withdraw(withdrawAmount, description);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Contains.Substring("Insufficient funds"));
        Assert.That(_account.Balance.Amount, Is.EqualTo(1000m)); // Balance unchanged
    }
}

// ❌ Bad: Poor test structure
[Test]
public void TestWithdraw()
{
    var account = new Account { Balance = 1000 };
    account.Withdraw(500); // No clear arrange/act/assert
    Assert.True(account.Balance == 500); // Magic numbers, unclear intent
}
```

#### 7. Configuration Review

**IOptions Pattern Usage**

```csharp
// ✅ Good: Proper configuration with IOptions
public class EmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.Port)
        {
            EnableSsl = _emailOptions.EnableSsl,
            Credentials = new NetworkCredential(_emailOptions.Username, _emailOptions.Password)
        };

        var message = new MailMessage(_emailOptions.FromAddress, to, subject, body);
        await client.SendMailAsync(message);

        _logger.LogInformation("Email sent to {Recipient} with subject {Subject}", to, subject);
    }
}

// ❌ Bad: Direct IConfiguration usage
public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var smtpServer = _configuration["Email:SmtpServer"]; // Runtime lookup
        var port = int.Parse(_configuration["Email:Port"]); // Potential runtime error

        // Configuration scattered throughout method
    }
}
```

## Review Checklist

### Reviewer Checklist

**Architecture & Design**

- [ ] Follows Clean Architecture principles
- [ ] Proper separation of concerns
- [ ] SOLID principles applied
- [ ] Domain logic encapsulated in entities
- [ ] Dependencies point inward

**Code Quality**

- [ ] Meaningful names for classes, methods, variables
- [ ] Methods are small and focused (< 20 lines)
- [ ] No code duplication
- [ ] Consistent coding style
- [ ] Appropriate comments and documentation

**Error Handling**

- [ ] Result pattern used consistently
- [ ] Proper exception handling
- [ ] Meaningful error messages
- [ ] Logging at appropriate levels
- [ ] Graceful degradation

**Security**

- [ ] Input validation implemented
- [ ] No hardcoded secrets
- [ ] Proper authentication/authorization
- [ ] Sensitive data protected
- [ ] SQL injection prevention

**Performance**

- [ ] Async/await used properly
- [ ] Database queries optimized
- [ ] Appropriate caching strategy
- [ ] Resource disposal handled
- [ ] Cancellation tokens used

**Testing**

- [ ] Unit tests cover business logic
- [ ] Test methods follow AAA pattern
- [ ] Edge cases covered
- [ ] Test names are descriptive
- [ ] Mocks used appropriately

**Configuration**

- [ ] IOptions pattern used
- [ ] Configuration models validated
- [ ] No direct IConfiguration usage
- [ ] Environment-specific settings
- [ ] Secrets properly managed

### Common Code Smells

**Long Methods**

```csharp
// ❌ Bad: Method doing too much
public async Task<Result> ProcessAccountTransactionAsync(CreateTransactionCommand command)
{
    // 50+ lines of validation, business logic, database calls, event publishing
    // This should be broken into smaller methods
}

// ✅ Good: Broken into focused methods
public async Task<Result> ProcessAccountTransactionAsync(CreateTransactionCommand command)
{
    var validationResult = await ValidateTransactionAsync(command);
    if (!validationResult.IsSuccess) return validationResult;

    var account = await GetAccountAsync(command.AccountId);
    var transactionResult = ProcessTransaction(account, command);

    await SaveChangesAsync(account);
    await PublishEventsAsync(account.DomainEvents);

    return Result.Success();
}
```

**Feature Envy**

```csharp
// ❌ Bad: Class accessing too much data from another class
public class TransactionValidator
{
    public bool IsValid(Transaction transaction)
    {
        return transaction.Account.Balance >= transaction.Amount &&
               transaction.Account.Status == AccountStatus.Active &&
               transaction.Account.Customer.IsVerified;
    }
}

// ✅ Good: Behavior belongs in the domain
public class Account
{
    public bool CanProcessTransaction(decimal amount)
    {
        return Balance >= amount &&
               Status == AccountStatus.Active &&
               Customer.IsVerified;
    }
}
```

**God Objects**

```csharp
// ❌ Bad: Class with too many responsibilities
public class AccountManager
{
    public void CreateAccount() { }
    public void ValidateAccount() { }
    public void SendNotification() { }
    public void LogActivity() { }
    public void ProcessPayment() { }
    public void GenerateReport() { }
}

// ✅ Good: Separated responsibilities
public class AccountService { /* Account operations */ }
public class AccountValidator { /* Validation */ }
public class NotificationService { /* Notifications */ }
public class AuditService { /* Logging */ }
```

## Review Tools and Automation

### Static Code Analysis

```xml
<!-- Enable analyzers in project file -->
<PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
    <CodeAnalysisRuleSet>../../analyzers/bank-system.ruleset</CodeAnalysisRuleSet>
</PropertyGroup>

<ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" PrivateAssets="all" />
    <PackageReference Include="SonarAnalyzer.CSharp" Version="9.16.0.82469" PrivateAssets="all" />
</ItemGroup>
```

### EditorConfig Settings

```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Code style rules
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Suggest more modern language features
csharp_prefer_simple_using_statement = true
csharp_prefer_braces = true
csharp_style_namespace_declarations = file_scoped
```

## Review Communication

### Constructive Feedback Examples

**Good Feedback**

```
"Consider using the Result pattern here instead of throwing exceptions for business rule violations. This would make the error handling more consistent with the rest of the codebase and easier to test."

"This method is doing multiple things. Could we extract the validation logic into a separate method to improve readability and testability?"

"Great use of the repository pattern! Consider adding a cancellation token parameter to support request cancellation."
```

**Poor Feedback**

```
"This is wrong." // Not constructive
"Just use exceptions." // No explanation
"Bad code." // Not helpful
```

### Review Priorities

1. **Security Issues** - Highest priority
2. **Correctness** - Logic errors, edge cases
3. **Performance** - Significant performance impacts
4. **Maintainability** - Code structure, readability
5. **Style** - Naming, formatting (lowest priority)

## Summary

1. **Focus on architecture and design** - Ensure Clean Architecture principles
2. **Verify domain logic** - Business rules should be in domain entities
3. **Check error handling** - Consistent Result pattern usage
4. **Review security** - Input validation and authorization
5. **Assess performance** - Async patterns and database queries
6. **Validate testing** - Comprehensive unit test coverage
7. **Examine configuration** - Proper IOptions usage
8. **Provide constructive feedback** - Be specific and helpful
9. **Prioritize issues** - Security and correctness first
10. **Use automation** - Leverage static analysis tools
