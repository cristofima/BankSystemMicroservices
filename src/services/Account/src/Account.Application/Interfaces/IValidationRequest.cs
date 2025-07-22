namespace BankSystem.Account.Application.Interfaces;

/// <summary>
/// Represents a request that can provide a custom title for validation errors.
/// </summary>
public interface IValidationRequest
{
    /// <summary>
    /// Gets the title for the validation error response.
    /// </summary>
    string ValidationErrorTitle { get; }
}