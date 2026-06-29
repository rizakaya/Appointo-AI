namespace Appointo.Core;

public sealed record OperationResult<T>(bool Success, T? Value, string Message)
{
    public static OperationResult<T> Ok(T value, string message) => new(true, value, message);
    public static OperationResult<T> Fail(string message) => new(false, default, message);
}
