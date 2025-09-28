using AutoMapper;
using BankSystem.Security.Api.Protos;
using BankSystem.Shared.Domain.Validation;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Security.Application.Features.Users.Queries;
using Security.Domain.Interfaces;

namespace Security.Api.Services;

/// <summary>
/// gRPC service for inter-service communication to retrieve user contact information.
/// This service acts as a thin orchestrator, delegating business logic to application layer handlers.
/// This service is secured and only accessible by authenticated microservices.
/// Uses inter-service authentication (API Key in development, mTLS in production).
/// </summary>
[Authorize(Policy = "InterServiceApiKey")]
public class UserContactGrpcService : UserContactService.UserContactServiceBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IGrpcValidationService _grpcValidationService;
    private readonly ILogger<UserContactGrpcService> _logger;

    public UserContactGrpcService(
        IMediator mediator,
        IMapper mapper,
        IGrpcValidationService grpcValidationService,
        ILogger<UserContactGrpcService> logger
    )
    {
        Guard.AgainstNull(mediator);
        Guard.AgainstNull(mapper);
        Guard.AgainstNull(grpcValidationService);
        Guard.AgainstNull(logger);

        _mediator = mediator;
        _mapper = mapper;
        _grpcValidationService = grpcValidationService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves user contact information by customer ID for inter-service communication
    /// </summary>
    public override async Task<GetUserContactResponse> GetUserContactByCustomerId(
        GetUserContactRequest request,
        ServerCallContext context
    )
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var (isValid, parsedId, errorMessage) =
                _grpcValidationService.ValidateAndParseCustomerId(request.CustomerId);
            if (!isValid)
            {
                _logger.LogWarning(
                    "Invalid request: {ErrorMessage}. CorrelationId: {CorrelationId}",
                    errorMessage,
                    correlationId
                );
                return new GetUserContactResponse { Success = false, ErrorMessage = errorMessage };
            }

            _logger.LogInformation(
                "Processing GetUserContactByCustomerId request for CustomerId: {ParsedId}. CorrelationId: {CorrelationId}",
                parsedId,
                correlationId
            );

            // Execute query using Clean Architecture pattern
            var query = new GetUserContactByCustomerIdQuery(parsedId);
            var result = await _mediator.Send(query, context.CancellationToken);

            if (result is { IsSuccess: true, Value: not null })
            {
                _logger.LogInformation(
                    "Successfully retrieved user contact for CustomerId: {ParsedId}. CorrelationId: {CorrelationId}",
                    parsedId,
                    correlationId
                );

                // Use AutoMapper to map from UserContactDto to UserContactInfo proto message
                var userContactInfo = _mapper.Map<UserContactInfo>(result.Value);

                return new GetUserContactResponse { Success = true, Data = userContactInfo };
            }

            _logger.LogWarning(
                "User contact not found for CustomerId: {ParsedId}. CorrelationId: {CorrelationId}",
                parsedId,
                correlationId
            );
            return new GetUserContactResponse { Success = false, ErrorMessage = result.Error };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing GetUserContactByCustomerId request for CustomerId: {CustomerId}. CorrelationId: {CorrelationId}",
                request.CustomerId,
                correlationId
            );

            return new GetUserContactResponse
            {
                Success = false,
                ErrorMessage = "An internal error occurred while processing your request",
            };
        }
    }

    /// <summary>
    /// Retrieves multiple user contacts by customer IDs for batch operations
    /// </summary>
    public override async Task<GetUserContactsBatchResponse> GetUserContactsByCustomerIds(
        GetUserContactsBatchRequest request,
        ServerCallContext context
    )
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var (isValid, validIds, errorMessage) = _grpcValidationService.ValidateCustomerIds(
                request.CustomerIds
            );

            if (!isValid)
            {
                _logger.LogWarning(
                    "Invalid request: {ErrorMessage}. CorrelationId: {CorrelationId}",
                    errorMessage,
                    correlationId
                );
                return new GetUserContactsBatchResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                };
            }

            _logger.LogInformation(
                "Processing GetUserContactsByCustomerIds batch request for {Count} customers. CorrelationId: {CorrelationId}",
                validIds.Count,
                correlationId
            );

            // Execute query
            var query = new GetUserContactsByCustomerIdsQuery(validIds);
            var result = await _mediator.Send(query, context.CancellationToken);

            if (result is { IsSuccess: true, Value: not null })
            {
                // Use AutoMapper to map the collection directly to UserContactInfo proto messages
                var userContacts = _mapper.Map<IEnumerable<UserContactInfo>>(result.Value).ToList();

                _logger.LogInformation(
                    "Successfully retrieved {Count} user contacts from {RequestedCount} requested. CorrelationId: {CorrelationId}",
                    userContacts.Count,
                    validIds.Count,
                    correlationId
                );

                return new GetUserContactsBatchResponse { Success = true, Data = { userContacts } };
            }

            _logger.LogWarning(
                "Error retrieving user contacts batch: {Error}. CorrelationId: {CorrelationId}",
                result.Error,
                correlationId
            );
            return new GetUserContactsBatchResponse
            {
                Success = false,
                ErrorMessage = result.Error,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing GetUserContactsByCustomerIds batch request. CorrelationId: {CorrelationId}",
                correlationId
            );
            return new GetUserContactsBatchResponse
            {
                Success = false,
                ErrorMessage = "An internal error occurred while processing the batch request",
            };
        }
    }
}
