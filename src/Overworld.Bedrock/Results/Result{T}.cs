namespace Overworld.Bedrock.Results;

public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    internal Result(T value) => _value = value;

    internal Result(Error error) => _error = error;

    public bool IsSuccess => _error is null;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Result failed with '{_error!.Code}' and has no value.");

    public Error Error => _error
        ?? throw new InvalidOperationException("Result succeeded and has no error.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
