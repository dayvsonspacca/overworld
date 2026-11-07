using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

[Operation("themes-apply", "Makes a theme, dark or light, the one the shell, the apps and GTK apps wear, and signals them to wear it now.")]
public sealed class ApplyThemeOp(ThemeStore store, GtkSheet gtk, BeaconClient beacon) : IOperation<ApplyTheme, Theme>
{
    public async Task<Result<Theme>> ExecuteAsync(ApplyTheme input, CancellationToken cancellationToken)
    {
        var applied = await store.ApplyAsync(input.Name?.Trim(), input.Variant, cancellationToken);

        if (!applied.IsSuccess)
        {
            return applied;
        }

        var written = await gtk.WriteAsync(applied.Value.Name, applied.Value.Colors(applied.Value.Worn!.Value), cancellationToken);
        await beacon.LightAsync("theme", cancellationToken);

        return written.IsSuccess
            ? applied
            : written.Error with { Message = $"{applied.Value.Name} is applied, but not to GTK apps: {written.Error.Message}" };
    }
}
