namespace BankSystem.Shared.Domain.Validation;

/// <summary>
/// Provides guard clause utilities for input validation following clean code principles.
/// These methods help ensure defensive programming and fail-fast behavior.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Guards against null values.
    /// </summary>
    /// <typeparam name="T">The type of the value to check</typeparam>
    /// <param name="value">The value to check for null</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static void AgainstNull<T>(T value, string parameterName) where T : class?
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);
    }

    /// <summary>
    /// Guards against null or empty strings.
    /// </summary>
    /// <param name="value">The string value to check</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentException">Thrown when value is null or empty</exception>
    public static void AgainstNullOrEmpty(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty", parameterName);
    }

    /// <summary>
    /// Guards against null or empty collections.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="collection">The collection to check</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentException">Thrown when collection is null or empty</exception>
    public static void AgainstNullOrEmpty<T>(IEnumerable<T> collection, string parameterName)
    {
        if (collection == null || !collection.Any())
            throw new ArgumentException("Collection cannot be null or empty", parameterName);
    }

    /// <summary>
    /// Guards against negative values.
    /// </summary>
    /// <param name="value">The decimal value to check</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentException">Thrown when value is negative</exception>
    public static void AgainstNegative(decimal value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentException($"Value cannot be negative: {value}", parameterName);
    }

    /// <summary>
    /// Guards against zero or negative values.
    /// </summary>
    /// <param name="value">The decimal value to check</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentException">Thrown when value is zero or negative</exception>
    public static void AgainstZeroOrNegative(decimal value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentException($"Value must be positive: {value}", parameterName);
    }

    /// <summary>
    /// Guards against zero or negative values for integers.
    /// </summary>
    /// <param name="value">The integer value to check</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentException">Thrown when value is zero or negative</exception>
    public static void AgainstZeroOrNegative(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentException($"Value must be positive: {value}", parameterName);
    }

    /// <summary>
    /// Guards against values outside a specified range.
    /// </summary>
    /// <param name="value">The decimal value to check</param>
    /// <param name="min">The minimum allowed value (inclusive)</param>
    /// <param name="max">The maximum allowed value (inclusive)</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentException">Thrown when value is outside the range</exception>
    public static void AgainstInvalidRange(decimal value, decimal min, decimal max, string parameterName)
    {
        if (value < min || value > max)
            throw new ArgumentException($"Value {value} is not within range [{min}, {max}]", parameterName);
    }

    /// <summary>
    /// Guards against empty Guid values.
    /// </summary>
    /// <param name="value">The Guid value to check</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentException">Thrown when value is Guid.Empty</exception>
    public static void AgainstEmptyGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Guid cannot be empty", parameterName);
    }

    /// <summary>
    /// Guards against invalid enum values.
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value to check</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentException">Thrown when value is not a valid enum value</exception>
    public static void AgainstInvalidEnum<TEnum>(TEnum value, string parameterName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentException($"Invalid enum value: {value}", parameterName);
    }

    /// <summary>
    /// Guards against strings that exceed a maximum length.
    /// </summary>
    /// <param name="value">The string value to check</param>
    /// <param name="maxLength">The maximum allowed length</param>
    /// <param name="parameterName">The name of the parameter being checked</param>
    /// <exception cref="ArgumentException">Thrown when string exceeds maximum length</exception>
    public static void AgainstExcessiveLength(string value, int maxLength, string parameterName)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
            throw new ArgumentException($"String length {value.Length} exceeds maximum of {maxLength}", parameterName);
    }

    /// <summary>
    /// Guards against null values with a custom exception factory.
    /// </summary>
    /// <typeparam name="T">The type of the value to check</typeparam>
    /// <typeparam name="TException">The type of exception to throw</typeparam>
    /// <param name="value">The value to check for null</param>
    /// <param name="exceptionFactory">Factory function to create the exception</param>
    /// <exception cref="TException">The exception created by the factory</exception>
    public static void Against<T, TException>(T value, Func<TException> exceptionFactory) 
        where T : class 
        where TException : Exception
    {
        if (value == null)
            throw exceptionFactory();
    }

    /// <summary>
    /// Guards against a condition being true.
    /// </summary>
    /// <param name="condition">The condition to check</param>
    /// <param name="message">The error message if condition is true</param>
    /// <exception cref="ArgumentException">Thrown when condition is true</exception>
    public static void Against(bool condition, string message)
    {
        if (condition)
            throw new ArgumentException(message);
    }

    /// <summary>
    /// Guards against a condition being true with a custom exception.
    /// </summary>
    /// <typeparam name="TException">The type of exception to throw</typeparam>
    /// <param name="condition">The condition to check</param>
    /// <param name="exceptionFactory">Factory function to create the exception</param>
    /// <exception cref="TException">The exception created by the factory</exception>
    public static void Against<TException>(bool condition, Func<TException> exceptionFactory) 
        where TException : Exception
    {
        if (condition)
            throw exceptionFactory();
    }
}
