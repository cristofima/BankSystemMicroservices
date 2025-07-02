namespace Security.Domain.Common;

/// <summary>
/// Represents a result of an operation with success/failure indication
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);

    public static implicit operator Result(string error) => Failure(error);
}

/// <summary>
/// Represents a result of an operation with a value
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static new Result<T> Failure(string error) => new(false, default, error);

    public static implicit operator Result<T>(string error) => Failure(error);
    public static implicit operator Result<T>(T value) => Success(value);
}
