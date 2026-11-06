using Overworld.Bedrock.Results;

namespace Overworld.Allay.Metering;

public interface IMetered
{
    Task<Result<Usage>> UsageAsync(CancellationToken cancellationToken);
}
