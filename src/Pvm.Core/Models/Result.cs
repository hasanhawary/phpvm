namespace Pvm.Core.Models;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail without throwing exceptions for expected failures.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Gets the optional exception associated with the failure.
    /// </summary>
    public Exception? Exception { get; }

    protected Result(bool isSuccess, string error, Exception? exception = null)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException("A successful result cannot contain an error message.");
        }

        if (!isSuccess && string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException("A failed result must contain an error message.");
        }

        IsSuccess = isSuccess;
        Error = error;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Ok() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed result with an error message and optional exception.
    /// </summary>
    public static Result Fail(string message, Exception? exception = null) => new(false, message, exception);

    /// <summary>
    /// Creates a successful result containing a value.
    /// </summary>
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    /// <summary>
    /// Creates a failed result for a value type with an error message and optional exception.
    /// </summary>
    public static Result<T> Fail<T>(string message, Exception? exception = null) => Result<T>.Fail(message, exception);
}

/// <summary>
/// Represents the outcome of an operation that returns a value on success or an error on failure.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value if the operation succeeded. Throws an exception if accessed on a failed result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on a failed result.</exception>
    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException($"Cannot access Value on a failed result. Error: {Error}", Exception);
            }

            return _value!;
        }
    }

    private Result(bool isSuccess, T? value, string error, Exception? exception = null)
        : base(isSuccess, error, exception)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a successful result containing a value.
    /// </summary>
    public static Result<T> Ok(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(true, value, string.Empty);
    }

    /// <summary>
    /// Creates a failed result with an error message and optional exception.
    /// </summary>
    public static new Result<T> Fail(string message, Exception? exception = null) =>
        new(false, default, message, exception);
}
