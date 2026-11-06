using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

[Operation("themes-list", "Lists Overworld's themes with their colors, and which one the shell and the apps wear now.")]
public sealed class ListThemesOp(ThemeStore store) : IOperation<ListThemes, IReadOnlyList<Theme>>
{
    public Task<Result<IReadOnlyList<Theme>>> ExecuteAsync(ListThemes input, CancellationToken cancellationToken) =>
        store.ListAsync(cancellationToken);
}
