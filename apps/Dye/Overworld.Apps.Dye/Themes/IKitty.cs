using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

public interface IKitty
{
    Task<Result<string>> ReloadAsync(CancellationToken cancellationToken);
}
