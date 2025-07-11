using BankSystem.Account.Domain.Enums;
using BankSystem.Account.Domain.ValueObjects;

namespace BankSystem.Account.Domain.Repositories;

/// <summary>
/// Repository interface for Account aggregate operations.
/// Provides data access methods for account entities following DDD principles.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Retrieves an account by its unique identifier.
    /// </summary>
    /// <param name="id">The unique account identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The account if found, null otherwise</returns>
    Task<Entities.Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an account by its account number.
    /// </summary>
    /// <param name="accountNumber">The account number</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The account if found, null otherwise</returns>
    Task<Entities.Account?> GetByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all accounts for a specific customer.
    /// </summary>
    /// <param name="customerId">The customer identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Collection of customer accounts</returns>
    Task<IEnumerable<Entities.Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves accounts by status with pagination.
    /// </summary>
    /// <param name="status">The account status to filter by</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Paginated collection of accounts</returns>
    Task<IEnumerable<Entities.Account>> GetByStatusAsync(
        AccountStatus status, 
        int pageNumber = 1, 
        int pageSize = 50, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an account with the specified account number exists.
    /// </summary>
    /// <param name="accountNumber">The account number to check</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if account exists, false otherwise</returns>
    Task<bool> ExistsByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active accounts for a customer.
    /// </summary>
    /// <param name="customerId">The customer identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>Number of active accounts</returns>
    Task<int> GetActiveAccountCountForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new account to the repository.
    /// </summary>
    /// <param name="account">The account to add</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task AddAsync(Entities.Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing account in the repository.
    /// </summary>
    /// <param name="account">The account to update</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task UpdateAsync(Entities.Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an account from the repository.
    /// </summary>
    /// <param name="account">The account to remove</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task RemoveAsync(Entities.Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the account exists by its unique identifier.
    /// </summary>
    /// <param name="id">The account identifier</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if account exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
