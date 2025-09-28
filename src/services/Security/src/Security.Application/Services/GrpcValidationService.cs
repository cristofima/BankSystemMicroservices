using Security.Domain.Interfaces;

namespace Security.Application.Services;

/// <summary>
/// Implementation of gRPC validation service that handles request validation
/// </summary>
public class GrpcValidationService : IGrpcValidationService
{
    /// <inheritdoc/>
    public (bool isValid, Guid parsedId, string errorMessage) ValidateAndParseCustomerId(
        string customerId
    )
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return (false, Guid.Empty, "Customer ID cannot be null or empty");
        }

        if (!Guid.TryParse(customerId, out var parsedId))
        {
            return (false, Guid.Empty, $"Invalid Customer ID format: {customerId}");
        }

        return parsedId == Guid.Empty
            ? (false, Guid.Empty, "Customer ID cannot be empty GUID")
            : (true, parsedId, string.Empty);
    }

    /// <inheritdoc/>
    public (bool isValid, List<Guid> validIds, string errorMessage) ValidateCustomerIds(
        IEnumerable<string>? customerIds
    )
    {
        var validIds = new List<Guid>();

        if (customerIds == null)
        {
            return (false, validIds, "Customer IDs collection cannot be null");
        }

        var customerIdList = customerIds.ToList();

        if (customerIdList.Count == 0)
        {
            return (false, validIds, "Customer IDs collection cannot be empty");
        }

        if (customerIdList.Count > 100) // Reasonable limit to prevent abuse
        {
            return (false, validIds, "Too many customer IDs requested. Maximum allowed: 100");
        }

        foreach (var customerId in customerIdList)
        {
            var (isValid, parsedId, _) = ValidateAndParseCustomerId(customerId);
            if (isValid)
            {
                validIds.Add(parsedId);
            }
            else
            {
                return (false, validIds, "Some customer IDs are invalid");
            }
        }

        return (true, validIds, string.Empty);
    }
}
