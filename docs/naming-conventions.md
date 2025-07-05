# Naming Conventions and Code Organization

## Overview

This document provides comprehensive guidelines for naming conventions and code organization in the Bank System Microservices project, following .NET best practices and Clean Code principles.

## Naming Conventions

### General Principles

1. **Use intention-revealing names** - Names should clearly express what the code does
2. **Make meaningful distinctions** - Avoid noise words and number series
3. **Use pronounceable names** - Code is read more than written
4. **Use searchable names** - Avoid single letters except for loop counters
5. **Avoid mental mapping** - Don't force readers to translate acronyms or abbreviations

### Classes and Interfaces

```csharp
// ✅ Good: Clear, descriptive class names
public class TransactionService { }
public class AccountController { }
public class UserRepository { }
public class PaymentProcessor { }

// ✅ Good: Interface naming with 'I' prefix
public interface IAccountRepository { }
public interface ITransactionService { }
public interface IPaymentProcessor { }

// ✅ Good: Abstract classes
public abstract class BaseEntity { }
public abstract class DomainException : Exception { }

// ❌ Bad: Unclear or abbreviated names
public class TxnSvc { }          // Too abbreviated
public class Manager { }         // Too generic
public class Helper { }          // What kind of help?
public class Data { }           // What kind of data?
```

### Methods and Functions

```csharp
// ✅ Good: Verb-based method names that express action
public async Task<Result<Account>> CreateAccountAsync(CreateAccountCommand command) { }
public async Task<Transaction> ProcessTransactionAsync(decimal amount) { }
public bool IsValidAccountNumber(string accountNumber) { }
public decimal CalculateInterest(decimal principal, decimal rate) { }

// ✅ Good: Query methods express what they return
public async Task<Account?> GetAccountByIdAsync(Guid accountId) { }
public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime start, DateTime end) { }

// ✅ Good: Boolean methods start with 'Is', 'Has', 'Can', 'Should'
public bool IsActive() { }
public bool HasSufficientFunds(decimal amount) { }
public bool CanProcessTransaction() { }
public bool ShouldApplyFee() { }

// ❌ Bad: Unclear method names
public void DoStuff() { }         // What stuff?
public void Process() { }         // Process what?
public bool Check() { }          // Check what?
public void Handle() { }         // Handle what?
```

### Properties and Fields

```csharp
// ✅ Good: Properties use PascalCase
public class Account
{
    public Guid Id { get; private set; }
    public string AccountNumber { get; private set; }
    public Money Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}

// ✅ Good: Private fields use camelCase with underscore prefix
public class TransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<TransactionService> _logger;
    private readonly IMapper _mapper;
}

// ✅ Good: Constants use PascalCase
public static class TransactionLimits
{
    public const decimal MaxDailyAmount = 50000m;
    public const int MaxTransactionsPerDay = 100;
    public const string DefaultCurrency = "USD";
}

// ❌ Bad: Inconsistent naming
public class BadExample
{
    public string accNum { get; set; }        // Should be AccountNumber
    private IRepository repo;                 // Should be _repository
    public const decimal max_amt = 1000;      // Should be MaxAmount
}
```

### Variables and Parameters

```csharp
// ✅ Good: Descriptive variable names
public async Task ProcessPayment(decimal paymentAmount, string recipientAccountNumber)
{
    var sourceAccount = await GetAccountAsync(sourceAccountId);
    var destinationAccount = await GetAccountAsync(recipientAccountNumber);
    var transactionFee = CalculateTransactionFee(paymentAmount);
    var totalAmount = paymentAmount + transactionFee;

    // Use meaningful loop variables
    foreach (var transaction in pendingTransactions)
    {
        await ProcessTransactionAsync(transaction);
    }
}

// ✅ Good: Use descriptive names even for short-lived variables
var isValidTransaction = ValidateTransaction(transaction);
var hasEnoughBalance = account.Balance >= transaction.Amount;
var withinDailyLimit = CheckDailyLimit(account, transaction.Amount);

// ❌ Bad: Generic or single-letter names
public void BadMethod(decimal amt, string acc)  // amt = amount, acc = account
{
    var a = GetAccountAsync(acc);               // What is 'a'?
    var x = CalculateFee(amt);                  // What is 'x'?
    var temp = amt + x;                         // What is 'temp'?
}
```

### Enums

```csharp
// ✅ Good: Singular noun for enum name, descriptive values
public enum AccountStatus
{
    Active,
    Suspended,
    Closed,
    PendingActivation
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    Fee,
    Interest
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    BankTransfer,
    PayPal,
    ApplePay
}

// ❌ Bad: Plural names or unclear values
public enum AccountStatuses { }    // Should be singular
public enum Status                 // Too generic
{
    A,                            // Unclear
    B,                            // Unclear
    C                             // Unclear
}
```

### Namespaces

```csharp
// ✅ Good: Hierarchical, descriptive namespaces
namespace BankSystem.Account.Domain.Entities
namespace BankSystem.Account.Application.Commands
namespace BankSystem.Transaction.Infrastructure.Repositories
namespace BankSystem.Security.Api.Controllers
namespace BankSystem.Shared.Domain.ValueObjects

// ✅ Good: Feature-based organization
namespace BankSystem.Payments.Application.Services
namespace BankSystem.Notifications.Infrastructure.Email
namespace BankSystem.Reporting.Api.Controllers

// ❌ Bad: Generic or unclear namespaces
namespace BankSystem.Stuff
namespace BankSystem.Utilities
namespace BankSystem.Common.Things
```

## File Organization

### Project Structure

```
/ServiceName/
├── src/
│   ├── ServiceName.Api/          # Controllers, Program.cs, Middleware
│   ├── ServiceName.Application/  # Commands, Queries, Handlers, DTOs
│   ├── ServiceName.Domain/       # Entities, Value Objects, Events
│   └── ServiceName.Infrastructure/ # Repositories, External Services
└── tests/
    ├── ServiceName.Unit.Tests/
    └── ServiceName.Integration.Tests/
```

### File Naming Conventions

```csharp
// ✅ Good: File names match class names exactly
TransactionController.cs        → public class TransactionController
IAccountRepository.cs          → public interface IAccountRepository
CreateAccountCommand.cs        → public record CreateAccountCommand
AccountCreatedEvent.cs         → public record AccountCreatedEvent

// ✅ Good: Multiple related classes in same file (when appropriate)
Results.cs                     → Contains Result<T> and Result classes
Exceptions.cs                  → Contains custom exception classes
Constants.cs                   → Contains static constant classes
```

### Using Statements Organization

```csharp
// ✅ Good: Organized using statements
// File header with namespace
namespace BankSystem.Transaction.Application.Commands;

// 1. System namespaces first
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// 2. Microsoft namespaces
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

// 3. Third-party packages
using FluentValidation;
using MediatR;
using AutoMapper;

// 4. Local project references
using BankSystem.Shared.Domain.ValueObjects;
using BankSystem.Transaction.Domain.Entities;
using BankSystem.Transaction.Application.DTOs;

// Class declaration with proper spacing
public class CreateTransactionCommand : IRequest<TransactionDto>
{
    // Implementation
}
```

### Class Organization

```csharp
public class TransactionService
{
    // 1. Constants first
    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    // 2. Private fields
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<TransactionService> _logger;

    // 3. Constructor
    public TransactionService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ILogger<TransactionService> logger)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    // 4. Public properties
    public int ProcessedTransactionCount { get; private set; }

    // 5. Public methods
    public async Task<Result<Transaction>> ProcessTransactionAsync(CreateTransactionCommand command)
    {
        // Implementation
    }

    // 6. Private methods
    private async Task<bool> ValidateTransactionAsync(CreateTransactionCommand command)
    {
        // Implementation
    }

    private decimal CalculateTransactionFee(decimal amount)
    {
        // Implementation
    }
}
```

## Domain-Specific Naming

### Banking Domain Terms

```csharp
// ✅ Good: Use ubiquitous language from banking domain
public class Account
{
    public string AccountNumber { get; }      // Not "AccNum" or "Number"
    public Money Balance { get; }             // Not "Amount" or "Value"
    public string RoutingNumber { get; }      // Not "Route" or "Routing"
    public AccountType Type { get; }          // Checking, Savings, etc.
}

public class Transaction
{
    public decimal Amount { get; }            // Clear and specific
    public TransactionType Type { get; }      // Deposit, Withdrawal, etc.
    public string Description { get; }        // Not "Desc" or "Notes"
    public string ReferenceNumber { get; }    // Not "RefNum" or "Ref"
}

// ✅ Good: Business concepts as first-class objects
public class InterestRate { }
public class CreditLimit { }
public class OverdraftProtection { }
public class TransactionFee { }
```

### Common Abbreviations to Avoid

```csharp
// ❌ Bad: Abbreviated names
public class AcctMgr { }          // → AccountManager
public class TxnProc { }          // → TransactionProcessor
public class CustSvc { }          // → CustomerService
public class PaymtGateway { }     // → PaymentGateway

// Methods
public void ProcPmt() { }         // → ProcessPayment
public void CalcInt() { }         // → CalculateInterest
public void ValidAcct() { }       // → ValidateAccount
public void GetAcctInfo() { }     // → GetAccountInformation

// Properties
public string CustId { get; }     // → CustomerId
public decimal AcctBal { get; }   // → AccountBalance
public DateTime TxnDate { get; }  // → TransactionDate
```

## Specific Patterns

### Command and Query Names

```csharp
// ✅ Good: Command naming pattern
public record CreateAccountCommand(Guid CustomerId, string AccountType);
public record UpdateAccountStatusCommand(Guid AccountId, AccountStatus Status);
public record ProcessTransactionCommand(Guid AccountId, decimal Amount);
public record CloseAccountCommand(Guid AccountId, string Reason);

// ✅ Good: Query naming pattern
public record GetAccountByIdQuery(Guid AccountId);
public record GetTransactionsByAccountQuery(Guid AccountId, int Page, int PageSize);
public record GetCustomerAccountsQuery(Guid CustomerId);
public record SearchTransactionsQuery(string SearchTerm, DateTime? FromDate);
```

### Event Names

```csharp
// ✅ Good: Past tense for domain events
public record AccountCreatedEvent(Guid AccountId, Guid CustomerId, DateTime CreatedAt);
public record TransactionProcessedEvent(Guid TransactionId, Guid AccountId, decimal Amount);
public record AccountClosedEvent(Guid AccountId, string Reason, DateTime ClosedAt);
public record PaymentReceivedEvent(Guid PaymentId, decimal Amount, string Source);
```

### DTO and Response Names

```csharp
// ✅ Good: Clear DTO naming
public record AccountDto(Guid Id, string AccountNumber, decimal Balance, string Status);
public record TransactionDto(Guid Id, decimal Amount, string Type, DateTime Date);
public record CustomerDto(Guid Id, string FirstName, string LastName, string Email);

// ✅ Good: Response naming
public record CreateAccountResponse(Guid AccountId, string AccountNumber);
public record ProcessTransactionResponse(Guid TransactionId, decimal NewBalance);
public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
```

## Best Practices Summary

### Naming Guidelines

1. **Use intention-revealing names** that clearly express purpose
2. **Avoid mental mapping** - don't use abbreviations or acronyms
3. **Use searchable names** - avoid single letters except for loop counters
4. **Be consistent** - use the same naming pattern throughout the codebase
5. **Use domain language** - adopt terms from the business domain

### Organization Guidelines

1. **Group related functionality** together in the same namespace/folder
2. **Separate concerns** - keep different layers in different projects
3. **Use consistent file structure** across all microservices
4. **Organize using statements** in a logical order (System, Microsoft, Third-party, Local)
5. **Structure classes logically** (constants, fields, constructor, properties, methods)

### Code Readability

1. **Choose clarity over brevity** - `GetAccountByAccountNumber` is better than `GetAcctByNum`
2. **Use positive boolean names** - `IsActive` instead of `IsNotInactive`
3. **Avoid double negatives** - `IsEnabled` instead of `IsNotDisabled`
4. **Use verb-noun pairs** for methods - `CreateAccount`, `ProcessTransaction`
5. **Use noun phrases** for properties - `AccountBalance`, `TransactionDate`

Remember: Code is read far more often than it's written. Invest in names that make your code self-documenting and easy to understand for other developers (including your future self).
