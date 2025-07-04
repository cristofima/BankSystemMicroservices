# Clean Code Guidelines for .NET Development

## Overview

This document provides comprehensive clean code principles based on Robert C. Martin's "Clean Code: A Handbook of Agile Software Craftsmanship," specifically adapted for .NET development in the Bank System Microservices project. These guidelines should be followed by all developers and AI assistants when writing or reviewing code.

## Chapter 1: Clean Code Fundamentals

### Core Principles

- **The Boy Scout Rule**: Always leave the codebase cleaner than you found it
- **Readability First**: Code is read far more often than it is written
- **Single Focus**: Each function, class, and module should do one thing well
- **Test-Driven**: Clean code runs all tests and has comprehensive test coverage

### Implementation Guidelines

```csharp
// ✅ Good: Clean, focused, and readable
public class CustomerValidator
{
    public ValidationResult ValidateCustomer(Customer customer)
    {
        if (customer == null)
            return ValidationResult.Failure("Customer cannot be null");

        if (string.IsNullOrWhiteSpace(customer.Email))
            return ValidationResult.Failure("Email is required");

        return ValidationResult.Success();
    }
}

// ❌ Bad: Multiple responsibilities, unclear purpose
public class CustomerManager
{
    public void DoStuff(object data) // Unclear name and responsibility
    {
        // Mixed concerns: validation, processing, logging, email
        // ... complex mixed logic
    }
}
```

## Chapter 2: Meaningful Names

### Naming Rules for .NET

#### Use Intention-Revealing Names

```csharp
// ✅ Good: Clear intent
public decimal CalculateMonthlyInterest(decimal principal, decimal annualRate)
public List<Account> GetActiveAccountsByCustomer(Guid customerId)
public bool IsAccountEligibleForOverdraft(Account account)

// ❌ Bad: Unclear intent
public decimal calc(decimal d1, decimal d2)
public List<Account> getData(Guid id)
public bool check(Account acc)
```

#### Avoid Disinformation

```csharp
// ✅ Good: Accurate naming
public IList<Account> ActiveAccounts { get; set; }
public Dictionary<string, Customer> CustomerIndex { get; set; }

// ❌ Bad: Misleading names
public List<Account> AccountList { get; set; } // What if it's not a List<T>?
public Account[] CustomerArray { get; set; } // Misleading if it's not an array
```

#### Class and Method Naming Conventions

```csharp
// ✅ Good: Classes are nouns, methods are verbs
public class TransactionProcessor
{
    public void ProcessPayment(PaymentRequest request)
    public TransactionResult ValidateTransaction(Transaction transaction)
    public void SendNotification(string recipient, string message)
}

// ❌ Bad: Inappropriate naming patterns
public class ProcessTransaction // Should be a noun
{
    public void Transaction() // Should be a verb
    public string Data() // Non-descriptive
}
```

#### Use Searchable Names

```csharp
// ✅ Good: Searchable constants
public const int MaxTransactionsPerDay = 50;
public const decimal DailyWithdrawalLimit = 5000m;
public const string DefaultCurrency = "USD";

// ❌ Bad: Magic numbers and strings
if (transactions.Count > 50) // What is 50?
if (amount > 5000) // What is 5000?
```

## Chapter 3: Functions

### Function Design Rules

#### Keep Functions Small

```csharp
// ✅ Good: Small, focused function (under 20 lines)
public Result ValidateTransactionAmount(decimal amount, Account account)
{
    if (amount <= 0)
        return Result.Failure("Amount must be positive");

    if (amount > account.DailyLimit)
        return Result.Failure("Amount exceeds daily limit");

    if (account.Balance < amount)
        return Result.Failure("Insufficient funds");

    return Result.Success();
}

// ❌ Bad: Large function with multiple responsibilities
public Result ProcessTransaction(Transaction transaction)
{
    // 50+ lines of mixed validation, processing, logging, notification logic
}
```

#### Do One Thing

```csharp
// ✅ Good: Single responsibility
public class EmailService
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Only sends emails
    }
}

public class EmailValidator
{
    public bool IsValidEmail(string email)
    {
        // Only validates email format
    }
}

// ❌ Bad: Multiple responsibilities
public class EmailManager
{
    public async Task ProcessEmail(string email, string subject, string body)
    {
        // Validates, sends, logs, stores, and formats emails
    }
}
```

#### Minimize Function Arguments

```csharp
// ✅ Good: Few arguments, use parameter objects
public record TransactionRequest(
    Guid AccountId,
    decimal Amount,
    string Description,
    TransactionType Type);

public Result ProcessTransaction(TransactionRequest request)
{
    // Implementation
}

// ❌ Bad: Too many arguments
public Result ProcessTransaction(Guid accountId, decimal amount, string description,
    TransactionType type, DateTime date, string reference, bool requireApproval,
    decimal fee, string currency)
{
    // Too many parameters
}
```

#### Command Query Separation

```csharp
// ✅ Good: Separate commands and queries
public class AccountService
{
    // Query - returns data, no side effects
    public async Task<Account> GetAccountAsync(Guid accountId)
    {
        return await _repository.GetByIdAsync(accountId);
    }

    // Command - performs action, returns success/failure
    public async Task<Result> UpdateAccountStatusAsync(Guid accountId, AccountStatus status)
    {
        var account = await _repository.GetByIdAsync(accountId);
        account.UpdateStatus(status);
        await _repository.UpdateAsync(account);
        return Result.Success();
    }
}

// ❌ Bad: Mixed command and query
public async Task<Account> UpdateAndGetAccountAsync(Guid accountId, AccountStatus status)
{
    // Both modifies state AND returns data
}
```

## Chapter 4: Comments

### Comment Guidelines

#### Prefer Self-Documenting Code

```csharp
// ✅ Good: Code explains itself
public bool IsAccountEligibleForLoan(Account account)
{
    return account.IsActive &&
           account.CreditScore >= MinimumCreditScore &&
           account.MonthlyIncome >= MinimumIncomeRequirement;
}

// ❌ Bad: Needs comments to explain
public bool Check(Account a)
{
    // Check if account is active
    // Check credit score is above 650
    // Check monthly income is above 3000
    return a.Status == 1 && a.Score >= 650 && a.Income >= 3000;
}
```

#### When Comments Are Necessary

```csharp
// ✅ Good: Explains business rule or complex algorithm
public decimal CalculateCompoundInterest(decimal principal, decimal rate, int periods)
{
    // Using the compound interest formula: A = P(1 + r/n)^(nt)
    // This implements the regulatory requirement for daily compounding
    return principal * Math.Pow(1 + (rate / 365), periods);
}

// ✅ Good: Explains intent for non-obvious code
public void ProcessTransactions()
{
    // Process in batches of 100 to avoid memory issues with large datasets
    const int batchSize = 100;
    // Implementation
}
```

#### Avoid These Comment Types

```csharp
// ❌ Bad: Redundant comments
public class Customer
{
    // Constructor for Customer class
    public Customer() { }

    // Gets the customer name
    public string GetName() { return _name; }
}

// ❌ Bad: Commented-out code
public void ProcessPayment()
{
    // var oldMethod = CalculateOldWay(); // Don't do this
    var result = CalculateNewWay();
}
```

## Chapter 5: Formatting

### Vertical Formatting

```csharp
// ✅ Good: Proper vertical spacing and organization
public class TransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(ITransactionRepository repository, ILogger<TransactionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Transaction>> CreateTransactionAsync(CreateTransactionCommand command)
    {
        var validation = ValidateCommand(command);
        if (!validation.IsSuccess)
            return Result<Transaction>.Failure(validation.Error);

        var transaction = CreateTransactionFromCommand(command);

        await _repository.AddAsync(transaction);
        _logger.LogInformation("Transaction {TransactionId} created", transaction.Id);

        return Result<Transaction>.Success(transaction);
    }

    private ValidationResult ValidateCommand(CreateTransactionCommand command)
    {
        // Validation logic
    }

    private Transaction CreateTransactionFromCommand(CreateTransactionCommand command)
    {
        // Creation logic
    }
}
```

### Horizontal Formatting

```csharp
// ✅ Good: Consistent indentation and line length
public class PaymentProcessor
{
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var validation = await ValidatePaymentRequestAsync(request);
        if (!validation.IsValid)
            return PaymentResult.Failure(validation.Errors);

        return await ExecutePaymentAsync(request);
    }
}

// ❌ Bad: Inconsistent formatting
public class PaymentProcessor{
public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request){
if(request==null)throw new ArgumentNullException(nameof(request));
var validation=await ValidatePaymentRequestAsync(request);
if(!validation.IsValid)return PaymentResult.Failure(validation.Errors);
return await ExecutePaymentAsync(request);}}
```

## Chapter 6: Objects and Data Structures

### Data Abstraction

```csharp
// ✅ Good: Hides implementation, exposes behavior
public class Account
{
    private decimal _balance;
    private readonly List<Transaction> _transactions = new();

    public Money Balance => new Money(_balance);
    public bool IsOverdrawn => _balance < 0;

    public Result Withdraw(decimal amount)
    {
        if (amount > _balance)
            return Result.Failure("Insufficient funds");

        _balance -= amount;
        _transactions.Add(new Transaction(TransactionType.Withdrawal, amount));
        return Result.Success();
    }
}

// ❌ Bad: Exposes data structure
public class Account
{
    public decimal Balance { get; set; }
    public List<Transaction> Transactions { get; set; }
}
```

### Law of Demeter

```csharp
// ✅ Good: Follows Law of Demeter
public class OrderService
{
    public void ProcessOrder(Order order)
    {
        var total = order.CalculateTotal(); // Talk to immediate neighbor
        var customer = order.GetCustomer(); // Talk to immediate neighbor

        // Don't chain calls
        NotifyCustomer(customer, total);
    }

    private void NotifyCustomer(Customer customer, decimal total)
    {
        customer.SendNotification($"Order total: {total}");
    }
}

// ❌ Bad: Violates Law of Demeter
public class OrderService
{
    public void ProcessOrder(Order order)
    {
        // Too much knowledge of internal structure
        var email = order.GetCustomer().GetContactInfo().GetEmail();
        var phone = order.GetCustomer().GetContactInfo().GetPhone();
    }
}
```

## Chapter 7: Error Handling

### Use Exceptions, Not Return Codes

```csharp
// ✅ Good: Using Result pattern for business logic errors
public class AccountService
{
    public async Task<Result<Account>> CreateAccountAsync(CreateAccountCommand command)
    {
        if (await _repository.ExistsByEmailAsync(command.Email))
            return Result<Account>.Failure("Account with this email already exists");

        var account = Account.Create(command.Email, command.InitialDeposit);
        await _repository.AddAsync(account);

        return Result<Account>.Success(account);
    }
}

// ✅ Good: Using exceptions for system errors
public class DatabaseRepository
{
    public async Task<Account> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.Accounts.FindAsync(id)
                ?? throw new NotFoundException($"Account {id} not found");
        }
        catch (SqlException ex)
        {
            throw new DataAccessException("Database error occurred", ex);
        }
    }
}
```

### Don't Return Null

```csharp
// ✅ Good: Use Result pattern or throw exceptions
public async Task<Result<Customer>> GetCustomerByEmailAsync(string email)
{
    var customer = await _repository.GetByEmailAsync(email);
    return customer != null
        ? Result<Customer>.Success(customer)
        : Result<Customer>.Failure("Customer not found");
}

// ✅ Good: Use Null Object pattern
public class NullCustomer : Customer
{
    public override string Name => "Unknown Customer";
    public override bool IsValid => false;
}

// ❌ Bad: Returning null
public async Task<Customer> GetCustomerByEmailAsync(string email)
{
    return await _repository.GetByEmailAsync(email); // Might return null
}
```

## Chapter 8: Boundaries

### Encapsulate Third-Party Dependencies

```csharp
// ✅ Good: Wrapper for third-party service
public interface IEmailService
{
    Task<EmailResult> SendEmailAsync(string to, string subject, string body);
}

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;

    public SendGridEmailService(ISendGridClient sendGridClient)
    {
        _sendGridClient = sendGridClient;
    }

    public async Task<EmailResult> SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var message = MailHelper.CreateSingleEmail(
                new EmailAddress("noreply@bank.com"),
                new EmailAddress(to),
                subject,
                body,
                body);

            var response = await _sendGridClient.SendEmailAsync(message);

            return response.IsSuccessStatusCode
                ? EmailResult.Success()
                : EmailResult.Failure("Failed to send email");
        }
        catch (Exception ex)
        {
            return EmailResult.Failure($"Email service error: {ex.Message}");
        }
    }
}
```

## Chapter 9: Unit Tests

### F.I.R.S.T. Principles

```csharp
// ✅ Good: Fast, Independent, Repeatable, Self-Validating, Timely
[TestFixture]
public class AccountServiceTests
{
    private AccountService _accountService;
    private Mock<IAccountRepository> _mockRepository;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IAccountRepository>();
        _accountService = new AccountService(_mockRepository.Object);
    }

    [Test]
    public async Task CreateAccount_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateAccountCommand("test@email.com", 1000m);
        _mockRepository.Setup(r => r.ExistsByEmailAsync(command.Email))
                      .ReturnsAsync(false);

        // Act
        var result = await _accountService.CreateAccountAsync(command);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Email, Is.EqualTo(command.Email));
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
    }

    [Test]
    public async Task CreateAccount_WithExistingEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateAccountCommand("existing@email.com", 1000m);
        _mockRepository.Setup(r => r.ExistsByEmailAsync(command.Email))
                      .ReturnsAsync(true);

        // Act
        var result = await _accountService.CreateAccountAsync(command);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Contains.Substring("already exists"));
    }
}
```

## Chapter 10: Classes

### Single Responsibility Principle

```csharp
// ✅ Good: Each class has one responsibility
public class PasswordValidator
{
    public ValidationResult ValidatePassword(string password)
    {
        // Only validates passwords
    }
}

public class PasswordHasher
{
    public string HashPassword(string password)
    {
        // Only hashes passwords
    }
}

public class UserAuthenticator
{
    private readonly PasswordValidator _validator;
    private readonly PasswordHasher _hasher;

    public AuthenticationResult Authenticate(string username, string password)
    {
        // Only handles authentication logic
    }
}

// ❌ Bad: Multiple responsibilities
public class UserManager
{
    public void CreateUser() { } // User creation
    public void ValidatePassword() { } // Validation
    public void SendEmail() { } // Email service
    public void LogActivity() { } // Logging
    public void CalculateInterest() { } // Business logic
}
```

### High Cohesion

```csharp
// ✅ Good: High cohesion - all methods work with the same data
public class BankAccount
{
    private decimal _balance;
    private readonly List<Transaction> _transactions;
    private readonly string _accountNumber;

    public decimal Balance => _balance;
    public string AccountNumber => _accountNumber;

    public void Deposit(decimal amount)
    {
        _balance += amount;
        _transactions.Add(new Transaction(TransactionType.Deposit, amount));
    }

    public bool Withdraw(decimal amount)
    {
        if (_balance >= amount)
        {
            _balance -= amount;
            _transactions.Add(new Transaction(TransactionType.Withdrawal, amount));
            return true;
        }
        return false;
    }

    public IReadOnlyList<Transaction> GetTransactionHistory()
    {
        return _transactions.AsReadOnly();
    }
}
```

## Chapter 11: Systems

### Dependency Injection

```csharp
// ✅ Good: Proper dependency injection setup
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register services
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<IEmailService, SendGridEmailService>();

        var app = builder.Build();
        app.Run();
    }
}

public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService; // Dependencies injected
    }
}
```

## Chapter 12: Emergence (Simple Design)

### Kent Beck's Four Rules

1. **Runs all tests**
2. **Contains no duplicate code**
3. **Expresses intent clearly**
4. **Minimizes classes and methods**

```csharp
// ✅ Good: Follows all four rules
public class TransactionValidator
{
    private const decimal MaximumDailyLimit = 10000m;

    public ValidationResult ValidateTransaction(Transaction transaction)
    {
        var amountValidation = ValidateAmount(transaction.Amount);
        if (!amountValidation.IsValid)
            return amountValidation;

        var limitValidation = ValidateDailyLimit(transaction);
        if (!limitValidation.IsValid)
            return limitValidation;

        return ValidationResult.Success();
    }

    private ValidationResult ValidateAmount(decimal amount)
    {
        return amount > 0
            ? ValidationResult.Success()
            : ValidationResult.Failure("Amount must be positive");
    }

    private ValidationResult ValidateDailyLimit(Transaction transaction)
    {
        return transaction.Amount <= MaximumDailyLimit
            ? ValidationResult.Success()
            : ValidationResult.Failure("Transaction exceeds daily limit");
    }
}
```

## Chapter 13: Concurrency

### Async/Await Best Practices

```csharp
// ✅ Good: Proper async implementation
public class TransactionService
{
    public async Task<Result<Transaction>> ProcessTransactionAsync(
        CreateTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(command.AccountId, cancellationToken);
        if (account == null)
            return Result<Transaction>.Failure("Account not found");

        var transaction = account.CreateTransaction(command.Amount, command.Description);

        await _repository.UpdateAsync(account, cancellationToken);
        await _eventPublisher.PublishAsync(new TransactionCreatedEvent(transaction.Id), cancellationToken);

        return Result<Transaction>.Success(transaction);
    }
}

// ❌ Bad: Blocking async calls
public Transaction ProcessTransaction(CreateTransactionCommand command)
{
    var result = ProcessTransactionAsync(command).Result; // Don't block!
    return result.Value;
}
```

## Code Smells and Refactoring Indicators

### Common Code Smells

1. **Long Method**: Methods longer than 20 lines
2. **Large Class**: Classes with too many responsibilities
3. **Long Parameter List**: More than 3-4 parameters
4. **Duplicate Code**: Repeated logic across methods/classes
5. **Data Clumps**: Groups of data that always appear together
6. **Feature Envy**: Method more interested in other class's data
7. **Inappropriate Intimacy**: Classes knowing too much about each other
8. **Comments**: Excessive commenting indicates unclear code

### Refactoring Techniques

```csharp
// Before: Long method with multiple responsibilities
public void ProcessOrder(Order order)
{
    // Validation (20 lines)
    // Calculation (15 lines)
    // Persistence (10 lines)
    // Notification (8 lines)
}

// After: Extracted methods with single responsibilities
public void ProcessOrder(Order order)
{
    ValidateOrder(order);
    var total = CalculateOrderTotal(order);
    SaveOrder(order);
    NotifyCustomer(order, total);
}

private void ValidateOrder(Order order) { /* validation logic */ }
private decimal CalculateOrderTotal(Order order) { /* calculation logic */ }
private void SaveOrder(Order order) { /* persistence logic */ }
private void NotifyCustomer(Order order, decimal total) { /* notification logic */ }
```

## Implementation Checklist

### Before Submitting Code

- [ ] Method names clearly express their intent
- [ ] Functions are small (< 20 lines) and do one thing
- [ ] Classes have single responsibility
- [ ] No duplicate code exists
- [ ] Comments explain "why," not "what"
- [ ] Error handling is consistent and appropriate
- [ ] Dependencies are properly injected
- [ ] Tests cover the main scenarios
- [ ] Code follows consistent formatting
- [ ] No magic numbers or strings

### Code Review Checklist

- [ ] Can I understand what this code does without comments?
- [ ] Are there any code smells present?
- [ ] Does each class/method have a single responsibility?
- [ ] Are dependencies properly abstracted?
- [ ] Is error handling appropriate and consistent?
- [ ] Are there sufficient tests?
- [ ] Is the code DRY (Don't Repeat Yourself)?
- [ ] Does the code follow SOLID principles?

## Summary

Clean code is not just about following rules—it's about crafting code that is:

- **Readable**: Easy to understand by other developers
- **Maintainable**: Easy to modify and extend
- **Testable**: Easy to write tests for
- **Robust**: Handles errors gracefully
- **Simple**: Uses the simplest design that works

Remember: Clean code is written by and for humans. While machines execute our code, humans must read, understand, and maintain it. Always optimize for the human reader first.
