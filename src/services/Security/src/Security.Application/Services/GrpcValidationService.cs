using BankSystem.Shared.Domain.Common;
using Security.Domain.Interfaces;

namespace Security.Application.Services;

/// <summary>
/// Implementation of gRPC validation service that handles request validation
/// </summary>
public class GrpcValidationService : IGrpcValidationService
{
    /// <inheritdoc/>
    public Result<Guid> ValidateAndParseCustomerId(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Result<Guid>.Failure("Customer ID cannot be null or empty");
        }

        if (!Guid.TryParse(customerId, out var parsedId))
        {
            return Result<Guid>.Failure($"Invalid Customer ID format: {customerId}");
        }

        return parsedId == Guid.Empty
            ? Result<Guid>.Failure("Customer ID cannot be empty GUID")
            : Result<Guid>.Success(parsedId);
    }

    /// <inheritdoc/>
    public Result<List<Guid>> ValidateCustomerIds(IEnumerable<string> customerIds)
    {
        var validIds = new List<Guid>();

        var customerIdList = customerIds.ToList();

        if (customerIdList.Count == 0)
        {
            return Result<List<Guid>>.Failure("Customer IDs collection cannot be empty");
        }

        if (customerIdList.Count > 100) // Reasonable limit to prevent abuse
        {
            return Result<List<Guid>>.Failure(
                "Too many customer IDs requested. Maximum allowed: 100"
            );
        }

        foreach (var result in customerIdList.Select(ValidateAndParseCustomerId))
        {
            if (result.IsFailure)
            {
                return Result<List<Guid>>.Failure(result.Error);
            }

            validIds.Add(result.Value);
        }

        return Result<List<Guid>>.Success(validIds);
    }
}
