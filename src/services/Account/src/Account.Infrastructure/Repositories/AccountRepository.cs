using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Domain.ValueObjects;
using BankSystem.Account.Infrastructure.Data;
using BankSystem.Shared.Domain.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Account aggregate operations using Entity Framework Core
/// </summary>
public class AccountRepository : IAccountRepository
{
    private readonly AccountDbContext _context;
    private readonly ILogger<AccountRepository> _logger;

    public AccountRepository(AccountDbContext context, ILogger<AccountRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AccountEntity?> GetByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    )
    {
        Guard.AgainstEmptyGuid(accountId, nameof(accountId));

        _logger.LogDebug("Retrieving account: {AccountId}", accountId);

        return await _context.Accounts.FirstOrDefaultAsync(
            a => a.Id == accountId,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AccountEntity>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default
    )
    {
        Guard.AgainstEmptyGuid(customerId, nameof(customerId));

        _logger.LogDebug("Retrieving accounts for customer: {CustomerId}", customerId);

        return await _context
            .Accounts.Where(a => a.CustomerId == customerId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(AccountEntity account, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(account, nameof(account));

        _logger.LogDebug("Adding new account: {AccountId}", account.Id);

        _context.Accounts.Add(account);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account {AccountId} added successfully", account.Id);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        AccountEntity account,
        CancellationToken cancellationToken = default
    )
    {
        Guard.AgainstNull(account, nameof(account));

        _logger.LogDebug("Updating account: {AccountId}", account.Id);

        _context.Accounts.Update(account);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account {AccountId} updated successfully", account.Id);
    }

    /// <inheritdoc />
    public async Task<bool> AccountNumberExistsAsync(
        string accountNumber,
        CancellationToken cancellationToken = default
    )
    {
        Guard.AgainstNullOrEmpty(accountNumber, nameof(accountNumber));

        _logger.LogDebug("Checking if account number exists: {AccountNumber}", accountNumber);

        return await _context.Accounts.AnyAsync(
            a => a.AccountNumber.Equals(new AccountNumber(accountNumber)),
            cancellationToken
        );
    }
}
