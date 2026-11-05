using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Operations;

public sealed class Operation<TInput, TOutput> : Operation
    where TInput : notnull
{
    private readonly IOperation<TInput, TOutput> _operation;

    internal Operation(string name, string description, IOperation<TInput, TOutput> operation)
        : base(name, description, typeof(TInput))
    {
        _operation = operation;
    }

    public Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken) =>
        _operation.ExecuteAsync(input, cancellationToken);

    public override async Task<Result<object?>> ExecuteAsync(object input, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync((TInput)input, cancellationToken);

        return result.IsSuccess
            ? Result.Success<object?>(result.Value)
            : Result.Failure<object?>(result.Error);
    }
}
