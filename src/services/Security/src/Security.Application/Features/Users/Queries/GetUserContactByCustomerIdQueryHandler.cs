using AutoMapper;
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
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserContactByCustomerIdQueryHandler> _logger;

    public GetUserContactByCustomerIdQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<GetUserContactByCustomerIdQueryHandler> logger
    )
    {
        Guard.AgainstNull(userRepository);
        Guard.AgainstNull(mapper);
        Guard.AgainstNull(logger);

        _userRepository = userRepository;
        _mapper = mapper;
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

            var userContactDto = _mapper.Map<UserContactDto>(user);

            _logger.LogDebug(
                "Successfully retrieved user contact for CustomerId: {CustomerId}",
                request.CustomerId
            );
            return Result<UserContactDto>.Success(userContactDto);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogDebug(
                ex,
                "Request cancelled while retrieving user contact for CustomerId: {CustomerId}",
                request.CustomerId
            );
            throw;
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
