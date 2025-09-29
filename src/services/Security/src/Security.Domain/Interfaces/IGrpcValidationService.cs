using BankSystem.Shared.Domain.Common;

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
    /// <returns>A Result containing the parsed Guid if successful, or an error message if validation fails</returns>
    Result<Guid> ValidateAndParseCustomerId(string customerId);

    /// <summary>
    /// Validates a list of customer IDs
    /// </summary>
    /// <param name="customerIds">The list of customer IDs to validate</param>
    /// <returns>A Result containing the list of valid parsed Guids if successful, or an error message if validation fails</returns>
    Result<List<Guid>> ValidateCustomerIds(IEnumerable<string> customerIds);
}
