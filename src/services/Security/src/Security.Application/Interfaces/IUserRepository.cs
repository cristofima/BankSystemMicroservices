using Security.Domain.Entities;

namespace Security.Application.Interfaces;

/// <summary>
/// Repository interface for user operations
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets user contact information by customer ID
    /// </summary>
    /// <param name="customerId">The customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information or null if not found</returns>
    Task<ApplicationUser?> GetUserByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets user contact information by multiple customer IDs
    /// </summary>
    /// <param name="customerIds">List of customer IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user information</returns>
    Task<IEnumerable<ApplicationUser>> GetUsersByCustomerIdsAsync(
        IEnumerable<Guid> customerIds,
        CancellationToken cancellationToken = default
    );
}
