namespace QuotesApi.Models;

public sealed class DomainResult<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess => Error is null;

    private DomainResult(T value) => Value = value;
    private DomainResult(string error) => Error = error;

    public static DomainResult<T> Ok(T value) => new(value);
    public static DomainResult<T> Fail(string error) => new(error);
}
