using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public interface IStep
{
    Task<Result<string>> RunAsync(CancellationToken cancellationToken);
}
