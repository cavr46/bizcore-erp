namespace BizCore.Domain.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, T? value, string? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException();
        if (!isSuccess && error == null)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);

    public Result<TU> Map<TU>(Func<T, TU> mapper)
    {
        return IsSuccess 
            ? Result<TU>.Success(mapper(Value!)) 
            : Result<TU>.Failure(Error!);
    }

    public async Task<Result<TU>> MapAsync<TU>(Func<T, Task<TU>> mapper)
    {
        return IsSuccess 
            ? Result<TU>.Success(await mapper(Value!)) 
            : Result<TU>.Failure(Error!);
    }
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException();
        if (!isSuccess && error == null)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}