using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

[Operation("themes-show", "Shows one theme with its colors, and whether it is the one worn now.")]
public sealed class ShowThemeOp(ThemeStore store) : IOperation<ShowTheme, Theme>
{
    public Task<Result<Theme>> ExecuteAsync(ShowTheme input, CancellationToken cancellationToken) =>
        store.FindAsync(input.Name.Trim(), cancellationToken);
}
