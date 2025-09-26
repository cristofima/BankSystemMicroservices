using FluentValidation;

namespace BankSystem.Shared.Application.Extensions;

/// <summary>
/// FluentValidation extensions for common validation scenarios
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates that a GUID is not empty (not Guid.Empty)
    /// </summary>
    /// <typeparam name="T">The type being validated</typeparam>
    /// <param name="ruleBuilder">The rule builder</param>
    /// <returns>The rule builder options for method chaining</returns>
    public static IRuleBuilderOptions<T, Guid> NotEmptyGuid<T>(
        this IRuleBuilder<T, Guid> ruleBuilder
    )
    {
        return ruleBuilder
            .NotEqual(Guid.Empty)
            .WithMessage("{PropertyName} is required and must be a valid GUID");
    }
}
