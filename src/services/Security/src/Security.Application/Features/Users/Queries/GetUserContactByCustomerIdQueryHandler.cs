using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Dtos;
using Security.Application.Interfaces;

namespace Security.Application.Features.Users.Queries;

/// <summary>
/// Handler for retrieving user contact information by customer ID
/// </summary>
public class GetUserContactByCustomerIdQueryHandler
    : IRequestHandler<GetUserContactByCustomerIdQuery, Result<UserContactDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserContactByCustomerIdQueryHandler> _logger;

    public GetUserContactByCustomerIdQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserContactByCustomerIdQueryHandler> logger
    )
    {
        Guard.AgainstNull(userRepository);
        Guard.AgainstNull(logger);

        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserContactDto>> Handle(
        GetUserContactByCustomerIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogDebug(
                "Processing GetUserContactByCustomerIdQuery for CustomerId: {CustomerId}",
                request.CustomerId
            );

            var user = await _userRepository.GetUserByCustomerIdAsync(
                request.CustomerId,
                cancellationToken
            );

            if (user == null)
            {
                _logger.LogWarning(
                    "User not found for CustomerId: {CustomerId}",
                    request.CustomerId
                );
                return Result<UserContactDto>.Failure("User not found");
            }

            var userContactDto = new UserContactDto
            {
                CustomerId = user.ClientId,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive =
                    !user.LockoutEnabled
                    || user.LockoutEnd == null
                    || user.LockoutEnd <= DateTimeOffset.UtcNow,
                CreatedAt = user.CreatedAt.DateTime,
                UpdatedAt = user.UpdatedAt?.DateTime,
            };

            _logger.LogDebug(
                "Successfully retrieved user contact for CustomerId: {CustomerId}",
                request.CustomerId
            );
            return Result<UserContactDto>.Success(userContactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving user contact for CustomerId: {CustomerId}",
                request.CustomerId
            );
            return Result<UserContactDto>.Failure(
                "An error occurred while retrieving user contact information"
            );
        }
    }
}
