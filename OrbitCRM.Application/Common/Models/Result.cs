namespace OrbitCRM.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public string? Error { get; protected set; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };

    public static Result<T> Success<T>(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure<T>(string error) => new() { IsSuccess = false, Error = error };
}

public class Result<T> : Result
{
    public T? Value { get; set; }
}