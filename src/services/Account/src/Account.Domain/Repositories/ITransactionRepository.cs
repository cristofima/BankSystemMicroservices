using BankSystem.Account.Domain.Entities;
using BankSystem.Account.Domain.Enums;

namespace BankSystem.Account.Domain.Repositories;

/// <summary>
/// Repository interface for managing Transaction entities and related data operations.
/// Provides methods for transaction persistence, retrieval, and querying operations.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Retrieves a transaction by its unique identifier.
    /// </summary>
    /// <param name="id">The unique transaction identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transaction if found, null otherwise</returns>
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transaction by its reference number.
    /// </summary>
    /// <param name="referenceNumber">The transaction reference number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transaction if found, null otherwise</returns>
    Task<Transaction?> GetByReferenceNumberAsync(string referenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all transactions for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of transactions for the account</returns>
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transactions for a specific account within a date range.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="fromDate">Start date for the range</param>
    /// <param name="toDate">End date for the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of transactions within the date range</returns>
    Task<IEnumerable<Transaction>> GetByAccountIdAndDateRangeAsync(
        Guid accountId, 
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transactions for a specific account with pagination.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated collection of transactions</returns>
    Task<IEnumerable<Transaction>> GetPagedByAccountIdAsync(
        Guid accountId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transactions by type for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="transactionType">The transaction type to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of transactions of the specified type</returns>
    Task<IEnumerable<Transaction>> GetByAccountIdAndTypeAsync(
        Guid accountId, 
        TransactionType transactionType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transactions by status for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="status">The transaction status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of transactions with the specified status</returns>
    Task<IEnumerable<Transaction>> GetByAccountIdAndStatusAsync(
        Guid accountId, 
        TransactionStatus status, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves pending transactions for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of pending transactions</returns>
    Task<IEnumerable<Transaction>> GetPendingByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves failed transactions for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of failed transactions</returns>
    Task<IEnumerable<Transaction>> GetFailedByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of transactions for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total number of transactions for the account</returns>
    Task<int> GetCountByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of transactions for a specific account within a date range.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="fromDate">Start date for the range</param>
    /// <param name="toDate">End date for the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total number of transactions within the date range</returns>
    Task<int> GetCountByAccountIdAndDateRangeAsync(
        Guid accountId, 
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the sum of transaction amounts for a specific account and transaction type.
    /// </summary>
    /// <param name="accountId">The account identifier</param>
    /// <param name="transactionType">The transaction type</param>
    /// <param name="fromDate">Start date for the calculation</param>
    /// <param name="toDate">End date for the calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sum of transaction amounts</returns>
    Task<decimal> GetSumByAccountIdAndTypeAsync(
        Guid accountId, 
        TransactionType transactionType, 
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new transaction to the repository.
    /// </summary>
    /// <param name="transaction">The transaction to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple transactions to the repository in a batch operation.
    /// </summary>
    /// <param name="transactions">The transactions to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing transaction in the repository.
    /// </summary>
    /// <param name="transaction">The transaction to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple transactions in the repository in a batch operation.
    /// </summary>
    /// <param name="transactions">The transactions to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a transaction from the repository.
    /// </summary>
    /// <param name="transaction">The transaction to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a transaction exists with the specified identifier.
    /// </summary>
    /// <param name="id">The transaction identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the transaction exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a transaction exists with the specified reference number.
    /// </summary>
    /// <param name="referenceNumber">The transaction reference number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the transaction exists, false otherwise</returns>
    Task<bool> ExistsByReferenceNumberAsync(string referenceNumber, CancellationToken cancellationToken = default);
}
