using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.Interfaces;

/// <summary>
/// Repository interface for Account aggregate operations
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Gets an account by its unique identifier
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The account if found, null otherwise</returns>
    //Task<AccountEntity?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an account by its account number
    /// </summary>
    /// <param name="accountNumber">The account number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The account if found, null otherwise</returns>
    Task<AccountEntity?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all accounts for a specific customer
    /// </summary>
    /// <param name="customerId">The customer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of accounts for the customer</returns>
    Task<IEnumerable<AccountEntity>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new account to the repository
    /// </summary>
    /// <param name="account">The account to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(AccountEntity account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing account
    /// </summary>
    /// <param name="account">The account to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(AccountEntity account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an account number is already in use
    /// </summary>
    /// <param name="accountNumber">The account number to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the account number exists, false otherwise</returns>
    Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default);
}
