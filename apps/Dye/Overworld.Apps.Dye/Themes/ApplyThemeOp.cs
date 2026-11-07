using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

[Operation("themes-apply", "Makes a theme, dark or light, the one the shell, the apps and GTK apps wear, and signals them to wear it now.")]
public sealed class ApplyThemeOp(ThemeStore store, GtkSheet gtk, IColorScheme scheme, BeaconClient beacon) : IOperation<ApplyTheme, Theme>
{
    public async Task<Result<Theme>> ExecuteAsync(ApplyTheme input, CancellationToken cancellationToken)
    {
        var applied = await store.ApplyAsync(input.Name?.Trim(), input.Variant, cancellationToken);

        if (!applied.IsSuccess)
        {
            return applied;
        }

        var variant = applied.Value.Worn!.Value;
        var written = await gtk.WriteAsync(applied.Value.Name, applied.Value.Colors(variant), cancellationToken);
        var preferred = await scheme.PreferAsync(variant, cancellationToken);
        await beacon.LightAsync("theme", cancellationToken);

        return (written.IsSuccess ? preferred : written) is { IsSuccess: false } failed
            ? failed.Error with { Message = $"{applied.Value.Name} is applied, but not to GTK apps: {failed.Error.Message}" }
            : applied;
    }
}
