using BankSystem.Shared.Domain.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Domain.Entities;
using Security.Infrastructure.Data;

namespace Security.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user-related database operations
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly SecurityDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(SecurityDbContext context, ILogger<UserRepository> logger)
    {
        Guard.AgainstNull(context);
        Guard.AgainstNull(logger);

        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a user by their customer ID
    /// </summary>
    /// <param name="customerId">The customer ID to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    public async Task<ApplicationUser?> GetUserByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Retrieving user by CustomerId: {CustomerId}", customerId);

            var user = await _context
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.ClientId == customerId, cancellationToken);

            _logger.LogDebug(
                user == null
                    ? "User not found for CustomerId: {CustomerId}"
                    : "User found for CustomerId: {CustomerId}",
                customerId
            );

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by CustomerId: {CustomerId}", customerId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves multiple users by their customer IDs
    /// </summary>
    /// <param name="customerIds">The collection of customer IDs to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of users found</returns>
    public async Task<IEnumerable<ApplicationUser>> GetUsersByCustomerIdsAsync(
        IEnumerable<Guid> customerIds,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var customerIdsList = customerIds.ToList();
            _logger.LogDebug("Retrieving users by {Count} customer IDs", customerIdsList.Count);

            if (customerIdsList.Count == 0)
            {
                _logger.LogDebug("No customer IDs provided, returning empty collection");
                return [];
            }

            // Limit batch size to prevent performance issues
            const int maxBatchSize = 100;
            if (customerIdsList.Count > maxBatchSize)
            {
                _logger.LogWarning(
                    "Batch size {Count} exceeds maximum allowed {MaxBatchSize}",
                    customerIdsList.Count,
                    maxBatchSize
                );
                throw new ArgumentException(
                    $"Batch size cannot exceed {maxBatchSize} items",
                    nameof(customerIds)
                );
            }

            var users = await _context
                .Users.AsNoTracking()
                .Where(u => customerIdsList.Contains(u.ClientId))
                .ToListAsync(cancellationToken);

            _logger.LogDebug(
                "Found {FoundCount} users from {RequestedCount} customer IDs",
                users.Count,
                customerIdsList.Count
            );

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users by customer IDs");
            throw;
        }
    }
}
