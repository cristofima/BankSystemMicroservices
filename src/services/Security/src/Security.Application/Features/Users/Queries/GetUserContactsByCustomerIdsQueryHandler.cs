using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Dtos;
using Security.Application.Interfaces;

namespace Security.Application.Features.Users.Queries;

/// <summary>
/// Handler for retrieving multiple user contacts by customer IDs
/// </summary>
public class GetUserContactsByCustomerIdsQueryHandler
    : IRequestHandler<GetUserContactsByCustomerIdsQuery, Result<IEnumerable<UserContactDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserContactsByCustomerIdsQueryHandler> _logger;

    public GetUserContactsByCustomerIdsQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserContactsByCustomerIdsQueryHandler> logger
    )
    {
        Guard.AgainstNull(userRepository);
        Guard.AgainstNull(logger);

        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<UserContactDto>>> Handle(
        GetUserContactsByCustomerIdsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var customerIds = request.CustomerIds.ToList();
            _logger.LogDebug(
                "Processing GetUserContactsByCustomerIdsQuery for {Count} customer IDs",
                customerIds.Count
            );

            if (customerIds.Count == 0)
            {
                _logger.LogWarning("Empty customer IDs list provided");
                return Result<IEnumerable<UserContactDto>>.Success([]);
            }

            // Limit batch size to prevent performance issues
            const int maxBatchSize = 100;
            if (customerIds.Count > maxBatchSize)
            {
                _logger.LogWarning(
                    "Batch size {Count} exceeds maximum allowed {MaxBatchSize}",
                    customerIds.Count,
                    maxBatchSize
                );
                return Result<IEnumerable<UserContactDto>>.Failure(
                    $"Batch size cannot exceed {maxBatchSize} items"
                );
            }

            var users = await _userRepository.GetUsersByCustomerIdsAsync(
                customerIds,
                cancellationToken
            );

            var userContactDtos = users
                .Select(u => new UserContactDto
                {
                    CustomerId = u.ClientId,
                    Email = u.Email ?? string.Empty,
                    FirstName = u.FirstName ?? string.Empty,
                    LastName = u.LastName ?? string.Empty,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    IsActive =
                        !u.LockoutEnabled
                        || u.LockoutEnd == null
                        || u.LockoutEnd <= DateTimeOffset.UtcNow,
                    CreatedAt = u.CreatedAt.DateTime,
                    UpdatedAt = u.UpdatedAt?.DateTime,
                })
                .ToList();

            _logger.LogDebug(
                "Successfully retrieved {FoundCount} user contacts from {RequestedCount} requested customer IDs",
                userContactDtos.Count,
                customerIds.Count
            );

            return Result<IEnumerable<UserContactDto>>.Success(userContactDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user contacts for batch request");
            return Result<IEnumerable<UserContactDto>>.Failure(
                "An error occurred while retrieving user contact information"
            );
        }
    }
}
