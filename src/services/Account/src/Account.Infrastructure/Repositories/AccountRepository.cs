using BankSystem.Account.Application.Interfaces;
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
    public async Task<AccountEntity?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmptyGuid(accountId, nameof(accountId));

        try
        {
            _logger.LogDebug("Retrieving account by ID: {AccountId}", accountId);

            return await _context.Accounts.FindAsync([accountId], cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account by ID {AccountId}", accountId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AccountEntity>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmptyGuid(customerId, nameof(customerId));

        try
        {
            _logger.LogDebug("Retrieving accounts for customer: {CustomerId}", customerId);

            return await _context.Accounts
                .Where(a => a.CustomerId == customerId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts for customer {CustomerId}", customerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(AccountEntity account, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(account, nameof(account));

        try
        {
            _logger.LogDebug("Adding new account: {AccountId}", account.Id);

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

    /// <inheritdoc />
    public async Task UpdateAsync(AccountEntity account, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(account, nameof(account));

        try
        {
            _logger.LogDebug("Updating account: {AccountId}", account.Id);

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

    /// <inheritdoc />
    public async Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNullOrEmpty(accountNumber, nameof(accountNumber));

        try
        {
            _logger.LogDebug("Checking if account number exists: {AccountNumber}", accountNumber);

            return await _context.Accounts
                .AnyAsync(a => a.AccountNumber.Value == accountNumber, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if account number exists {AccountNumber}", accountNumber);
            throw;
        }
    }
}
