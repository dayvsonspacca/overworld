using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

public interface IColorScheme
{
    Task<Result<string>> PreferAsync(Variant variant, CancellationToken cancellationToken);
}
