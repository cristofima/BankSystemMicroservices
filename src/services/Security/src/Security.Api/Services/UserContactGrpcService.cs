using System.Diagnostics;
using AutoMapper;
using BankSystem.Security.Api.Protos;
using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.WebApiDefaults.Constants;
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
[Authorize(Policy = InterServiceConstants.ApiKeyScheme)]
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
        var correlationId = GetCorrelationId(context);

        using var _ = _logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = correlationId }
        );

        try
        {
            var validationResult = _grpcValidationService.ValidateAndParseCustomerId(
                request.CustomerId
            );
            if (validationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Invalid request: {ErrorMessage}. CorrelationId: {CorrelationId}",
                    validationResult.Error,
                    correlationId
                );
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, validationResult.Error)
                );
            }

            _logger.LogInformation(
                "Processing GetUserContactByCustomerId request for CustomerId: {ParsedId}. CorrelationId: {CorrelationId}",
                validationResult.Value,
                correlationId
            );

            // Execute query using Clean Architecture pattern
            var query = new GetUserContactByCustomerIdQuery(validationResult.Value);
            var result = await _mediator.Send(query, context.CancellationToken);

            if (result is { IsSuccess: true, Value: not null })
            {
                _logger.LogDebug(
                    "Successfully retrieved user contact for CustomerId: {ParsedId}. CorrelationId: {CorrelationId}",
                    validationResult.Value,
                    correlationId
                );

                // Use AutoMapper to map from UserContactDto to UserContactInfo proto message
                var userContactInfo = _mapper.Map<UserContactInfo>(result.Value);

                return new GetUserContactResponse { Data = userContactInfo };
            }

            _logger.LogWarning(
                "User contact not found for CustomerId: {ParsedId}. CorrelationId: {CorrelationId}",
                result.Value,
                correlationId
            );

            context.ResponseTrailers.Add(HttpHeaderConstants.CorrelationId, correlationId);

            throw new RpcException(new Status(StatusCode.NotFound, result.Error));
        }
        catch (RpcException)
        {
            context.ResponseTrailers.Add(HttpHeaderConstants.CorrelationId, correlationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing GetUserContactByCustomerId request for CustomerId: {CustomerId}. CorrelationId: {CorrelationId}",
                request.CustomerId,
                correlationId
            );

            context.ResponseTrailers.Add(HttpHeaderConstants.CorrelationId, correlationId);

            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    "An internal error occurred while processing your request"
                )
            );
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
        var correlationId = GetCorrelationId(context);

        try
        {
            var validationResult = _grpcValidationService.ValidateCustomerIds(request.CustomerIds);

            if (validationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Invalid request: {ErrorMessage}. CorrelationId: {CorrelationId}",
                    validationResult.Error,
                    correlationId
                );
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, validationResult.Error)
                );
            }

            _logger.LogInformation(
                "Processing GetUserContactsByCustomerIds batch request for {Count} customers. CorrelationId: {CorrelationId}",
                validationResult.Value.Count,
                correlationId
            );

            // Execute query
            var query = new GetUserContactsByCustomerIdsQuery(validationResult.Value);
            var result = await _mediator.Send(query, context.CancellationToken);

            if (result is { IsSuccess: true, Value: not null })
            {
                // Use AutoMapper to map the collection directly to UserContactInfo proto messages
                var userContacts = _mapper.Map<IEnumerable<UserContactInfo>>(result.Value).ToList();

                _logger.LogDebug(
                    "Successfully retrieved {Count} user contacts from {RequestedCount} requested. CorrelationId: {CorrelationId}",
                    userContacts.Count,
                    validationResult.Value.Count,
                    correlationId
                );

                return new GetUserContactsBatchResponse { Data = { userContacts } };
            }

            _logger.LogWarning(
                "Error retrieving user contacts batch: {Error}. CorrelationId: {CorrelationId}",
                result.Error,
                correlationId
            );
            throw new RpcException(new Status(StatusCode.OutOfRange, result.Error));
        }
        catch (RpcException)
        {
            context.ResponseTrailers.Add(HttpHeaderConstants.CorrelationId, correlationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing GetUserContactsByCustomerIds batch request. CorrelationId: {CorrelationId}",
                correlationId
            );

            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    "An internal error occurred while processing the batch request"
                )
            );
        }
    }

    private string GetCorrelationId(ServerCallContext context)
    {
        return context
                .RequestHeaders.FirstOrDefault(h =>
                    h.Key.Equals(
                        HttpHeaderConstants.CorrelationId,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                ?.Value.ToString()
            ?? Activity.Current?.TraceId.ToString()
            ?? context.GetHttpContext()?.TraceIdentifier
            ?? Guid.NewGuid().ToString();
    }
}
