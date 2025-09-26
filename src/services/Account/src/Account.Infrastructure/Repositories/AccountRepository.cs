using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Infrastructure.Data;
using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Domain.ValueObjects;
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
        Guard.AgainstNull(context);
        Guard.AgainstNull(logger);

        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AccountEntity?> GetByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default
    )
    {
        Guard.AgainstEmptyGuid(accountId);

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
        Guard.AgainstEmptyGuid(customerId);

        _logger.LogDebug("Retrieving accounts for customer: {CustomerId}", customerId);

        return await _context
            .Accounts.Where(a => a.CustomerId == customerId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(AccountEntity account, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(account);

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
        Guard.AgainstNull(account);

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
        Guard.AgainstNullOrEmpty(accountNumber);
        var account = new AccountNumber(accountNumber);

        _logger.LogDebug(
            "Checking if account number exists: {AccountNumber}",
            account.GetMaskedValue()
        );

        return await _context.Accounts.AnyAsync(
            a => a.AccountNumber.Equals(account),
            cancellationToken
        );
    }
}
