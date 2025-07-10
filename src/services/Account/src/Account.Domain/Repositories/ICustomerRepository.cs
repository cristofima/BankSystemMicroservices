using Account.Domain.Entities;
using Account.Domain.ValueObjects;

namespace Account.Domain.Repositories;

/// <summary>
/// Repository interface for Customer aggregate operations.
/// Provides data access methods for customer entities following the Repository pattern.
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Retrieves a customer by their unique identifier.
    /// </summary>
    /// <param name="id">The unique customer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The customer if found, null otherwise</returns>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a customer by their email address.
    /// </summary>
    /// <param name="email">The customer's email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The customer if found, null otherwise</returns>
    Task<Customer?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a customer by their phone number.
    /// </summary>
    /// <param name="phoneNumber">The customer's phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The customer if found, null otherwise</returns>
    Task<Customer?> GetByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves customers by their name (first and/or last name).
    /// </summary>
    /// <param name="firstName">First name to search for (optional)</param>
    /// <param name="lastName">Last name to search for (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of customers matching the name criteria</returns>
    Task<IEnumerable<Customer>> GetByNameAsync(string? firstName = null, string? lastName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active customers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active customers</returns>
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves customers created within a specific date range.
    /// </summary>
    /// <param name="fromDate">Start date for the range</param>
    /// <param name="toDate">End date for the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of customers created within the date range</returns>
    Task<IEnumerable<Customer>> GetByCreationDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer exists with the specified email address.
    /// </summary>
    /// <param name="email">The email address to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a customer exists with the email, false otherwise</returns>
    Task<bool> ExistsByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer exists with the specified phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a customer exists with the phone number, false otherwise</returns>
    Task<bool> ExistsByPhoneNumberAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new customer to the repository.
    /// </summary>
    /// <param name="customer">The customer to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer in the repository.
    /// </summary>
    /// <param name="customer">The customer to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a customer from the repository.
    /// </summary>
    /// <param name="customer">The customer to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer exists with the specified identifier.
    /// </summary>
    /// <param name="id">The customer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the customer exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of customers in the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total number of customers</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active customers in the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of active customers</returns>
    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);
}
