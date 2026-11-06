using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

[Operation("themes-apply", "Makes a theme the one the shell and the apps wear, and signals them to wear it now.")]
public sealed class ApplyThemeOp(ThemeStore store, BeaconClient beacon) : IOperation<ApplyTheme, Theme>
{
    public async Task<Result<Theme>> ExecuteAsync(ApplyTheme input, CancellationToken cancellationToken)
    {
        var applied = await store.ApplyAsync(input.Name.Trim(), cancellationToken);

        if (applied.IsSuccess)
        {
            await beacon.LightAsync("theme", cancellationToken);
        }

        return applied;
    }
}
