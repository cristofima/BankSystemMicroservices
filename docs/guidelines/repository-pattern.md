# Repository Pattern Guidelines

## Overview

The Repository pattern encapsulates the logic needed to access data sources. It provides a more object-oriented view of the persistence layer and promotes testability by abstracting data access concerns.

## Basic Repository Interface

### Generic Repository Interface

```csharp
public interface IRepository<T, TId> where T : class
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}
```

### Specific Repository Interface

```csharp
public interface IAccountRepository : IRepository<Account, Guid>
{
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetActiveAccountsAsync(CancellationToken cancellationToken = default);
    Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Account>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
```

## Repository Implementation

### Entity Framework Implementation

```csharp
public class AccountRepository : IAccountRepository
{
    private readonly BankDbContext _context;
    private readonly ILogger<AccountRepository> _logger;

    public AccountRepository(BankDbContext context, ILogger<AccountRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account {AccountId}", id);
            throw;
        }
    }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNullOrEmpty(accountNumber, nameof(accountNumber));

        try
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account by number {AccountNumber}", accountNumber);
            throw;
        }
    }

    public async Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Accounts
                .Where(a => a.CustomerId == customerId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Accounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account with transactions {AccountId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Account>> GetActiveAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Accounts
                .Where(a => a.Status == AccountStatus.Active)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active accounts");
            throw;
        }
    }

    public async Task<PagedResult<Account>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var totalCount = await _context.Accounts.CountAsync(cancellationToken);

            var accounts = await _context.Accounts
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Account>
            {
                Data = accounts,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged accounts");
            throw;
        }
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(account, nameof(account));

        try
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account {AccountId} added successfully", account.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding account {AccountId}", account.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(account, nameof(account));

        try
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account {AccountId} updated successfully", account.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account {AccountId}", account.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Account account, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(account, nameof(account));

        try
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Account {AccountId} deleted successfully", account.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account {AccountId}", account.Id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Accounts
                .AnyAsync(a => a.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if account exists {AccountId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Accounts
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all accounts");
            throw;
        }
    }
}
```

## Query Repository Pattern (CQRS)

For read-only operations, consider separate query repositories:

```csharp
public interface IAccountQueryRepository
{
    Task<AccountDto?> GetAccountSummaryAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountListDto>> GetAccountsForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<PagedResult<AccountDto>> SearchAccountsAsync(AccountSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalBalanceForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public class AccountQueryRepository : IAccountQueryRepository
{
    private readonly BankDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AccountQueryRepository> _logger;

    public AccountQueryRepository(
        BankDbContext context,
        IMemoryCache cache,
        ILogger<AccountQueryRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AccountDto?> GetAccountSummaryAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"account_summary_{accountId}";

        if (_cache.TryGetValue(cacheKey, out AccountDto cachedAccount))
        {
            return cachedAccount;
        }

        try
        {
            var account = await _context.Accounts
                .AsNoTracking()
                .Where(a => a.Id == accountId)
                .Select(a => new AccountDto
                {
                    Id = a.Id,
                    AccountNumber = a.AccountNumber,
                    Balance = a.Balance.Amount,
                    Currency = a.Balance.Currency.Code,
                    Status = a.Status.ToString(),
                    CustomerId = a.CustomerId,
                    CreatedAt = a.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (account != null)
            {
                _cache.Set(cacheKey, account, TimeSpan.FromMinutes(5));
            }

            return account;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account summary {AccountId}", accountId);
            throw;
        }
    }

    public async Task<PagedResult<AccountDto>> SearchAccountsAsync(
        AccountSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Accounts.AsNoTracking().AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(criteria.AccountNumber))
            {
                query = query.Where(a => a.AccountNumber.Contains(criteria.AccountNumber));
            }

            if (criteria.CustomerId.HasValue)
            {
                query = query.Where(a => a.CustomerId == criteria.CustomerId.Value);
            }

            if (criteria.Status.HasValue)
            {
                query = query.Where(a => a.Status == criteria.Status.Value);
            }

            if (criteria.MinBalance.HasValue)
            {
                query = query.Where(a => a.Balance.Amount >= criteria.MinBalance.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and projection
            var accounts = await query
                .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .Select(a => new AccountDto
                {
                    Id = a.Id,
                    AccountNumber = a.AccountNumber,
                    Balance = a.Balance.Amount,
                    Currency = a.Balance.Currency.Code,
                    Status = a.Status.ToString(),
                    CustomerId = a.CustomerId,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<AccountDto>
            {
                Data = accounts,
                TotalCount = totalCount,
                PageNumber = criteria.PageNumber,
                PageSize = criteria.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching accounts with criteria {@Criteria}", criteria);
            throw;
        }
    }
}
```

## Specification Pattern Integration

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
    bool IsSatisfiedBy(T entity);
}

public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }
}

public class ActiveAccountSpecification : Specification<Account>
{
    public override Expression<Func<Account, bool>> ToExpression()
    {
        return account => account.Status == AccountStatus.Active;
    }
}

public class AccountsByCustomerSpecification : Specification<Account>
{
    private readonly Guid _customerId;

    public AccountsByCustomerSpecification(Guid customerId)
    {
        _customerId = customerId;
    }

    public override Expression<Func<Account, bool>> ToExpression()
    {
        return account => account.CustomerId == _customerId;
    }
}

// Repository method using specifications
public async Task<IEnumerable<Account>> FindAsync(ISpecification<Account> specification, CancellationToken cancellationToken = default)
{
    return await _context.Accounts
        .Where(specification.ToExpression())
        .ToListAsync(cancellationToken);
}
```

## Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    IAccountRepository AccountRepository { get; }
    ITransactionRepository TransactionRepository { get; }
    ICustomerRepository CustomerRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly BankDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(BankDbContext context)
    {
        _context = context;
        AccountRepository = new AccountRepository(context);
        TransactionRepository = new TransactionRepository(context);
        CustomerRepository = new CustomerRepository(context);
    }

    public IAccountRepository AccountRepository { get; }
    public ITransactionRepository TransactionRepository { get; }
    public ICustomerRepository CustomerRepository { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```

## Repository Registration

```csharp
// Program.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        // Query repositories
        services.AddScoped<IAccountQueryRepository, AccountQueryRepository>();
        services.AddScoped<ITransactionQueryRepository, TransactionQueryRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

## Testing Repositories

### Unit Testing with In-Memory Database

```csharp
public class AccountRepositoryTests : IDisposable
{
    private readonly BankDbContext _context;
    private readonly AccountRepository _repository;
    private readonly ILogger<AccountRepository> _logger;

    public AccountRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BankDbContext(options);
        _logger = Mock.Of<ILogger<AccountRepository>>();
        _repository = new AccountRepository(_context, _logger);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingAccount_ShouldReturnAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = Account.CreateNew("123456789", Guid.NewGuid(), new Money(1000, Currency.USD));
        account.Id = accountId;

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(accountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(accountId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentAccount_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

## Best Practices

### Repository Design

1. **Interface Segregation**: Create focused interfaces for specific needs
2. **Async Operations**: Use async methods for all database operations
3. **Cancellation Support**: Include CancellationToken parameters
4. **Guard Clauses**: Validate inputs before processing
5. **Logging**: Log all important operations and errors
6. **Exception Handling**: Let infrastructure exceptions bubble up

### Performance Considerations

1. **Projection**: Use Select() to retrieve only needed data
2. **AsNoTracking**: Use for read-only queries
3. **Caching**: Implement caching for frequently accessed data
4. **Pagination**: Always implement pagination for large result sets
5. **Include Strategy**: Be careful with Include() to avoid N+1 problems

### Testing Guidelines

1. **Mock Dependencies**: Mock external dependencies, not the repository itself
2. **In-Memory Database**: Use in-memory database for integration tests
3. **Test Edge Cases**: Test null inputs, empty results, exceptions
4. **Separate Concerns**: Test repository logic separately from business logic

---

_This guideline follows the principles outlined in [Clean Code Guidelines](./clean-code.md) and [SOLID Principles](./solid-principles.md)._
