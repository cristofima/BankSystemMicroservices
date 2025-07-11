namespace BankSystem.Account.Domain.Guards;

/// <summary>
/// Guard clauses for parameter validation.
/// </summary>
public static class Guard
{
    public static void AgainstNull<T>(T value, string parameterName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(parameterName);
    }

    public static void AgainstNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace", parameterName);
    }

    public static void AgainstEmptyGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Value cannot be empty GUID", parameterName);
    }
}