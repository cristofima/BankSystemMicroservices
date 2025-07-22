namespace BankSystem.Shared.Domain.Common;

/// <summary>
/// Represents a result of an operation with success/failure indication
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public ErrorType? ErrorType { get; }

    protected Result(bool isSuccess, string error, ErrorType? errorType = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error, ErrorType errorType = Common.ErrorType.Failure) => new(false, error, errorType);
}

/// <summary>
/// Represents a result of an operation with a value
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string error, ErrorType? errorType = null) : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public new static Result<T> Failure(string error, ErrorType errorType = Common.ErrorType.Failure) => new(false, default, error, errorType);
}
