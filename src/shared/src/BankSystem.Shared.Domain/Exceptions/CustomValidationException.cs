using BankSystem.Shared.Domain.Validation;

namespace BankSystem.Shared.Domain.Exceptions;

public class CustomValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public string Title { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomValidationException"/> class with specified validation errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="title">The title of the exception.</param>
    public CustomValidationException(
        IReadOnlyDictionary<string, string[]> errors,
        string title = "Validation Error"
    )
        : base("One or more validation errors occurred.")
    {
        Guard.AgainstNull(Errors);
        Guard.AgainstNull(Title);

        Errors = errors;
        Title = title;
    }
}
