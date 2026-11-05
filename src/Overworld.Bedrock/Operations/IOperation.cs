using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Operations;

public interface IOperation<in TInput, TOutput>
{
    Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken);
}
