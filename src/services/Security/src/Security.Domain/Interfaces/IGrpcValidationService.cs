namespace Security.Domain.Interfaces;

/// <summary>
/// Service responsible for validating gRPC requests and parameters
/// </summary>
public interface IGrpcValidationService
{
    /// <summary>
    /// Validates and parses a customer ID from string to Guid
    /// </summary>
    /// <param name="customerId">The customer ID as string</param>
    /// <returns>A tuple indicating if validation succeeded and the parsed Guid</returns>
    (bool isValid, Guid parsedId, string errorMessage) ValidateAndParseCustomerId(
        string customerId
    );

    /// <summary>
    /// Validates a list of customer IDs
    /// </summary>
    /// <param name="customerIds">The list of customer IDs to validate</param>
    /// <returns>A tuple with validation result, valid IDs, and error message</returns>
    (bool isValid, List<Guid> validIds, string errorMessage) ValidateCustomerIds(
        IEnumerable<string> customerIds
    );
}
