using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

[Operation("themes-apply", "Makes a theme, dark or light, the one the shell, the apps, GTK apps and kitty wear, and signals them to wear it now.")]
public sealed class ApplyThemeOp(ThemeStore store, GtkSheet gtk, IColorScheme scheme, KittyConf conf, IKitty kitty, BeaconClient beacon)
    : IOperation<ApplyTheme, Theme>
{
    public async Task<Result<Theme>> ExecuteAsync(ApplyTheme input, CancellationToken cancellationToken)
    {
        var applied = await store.ApplyAsync(input.Name?.Trim(), input.Variant, cancellationToken);

        if (!applied.IsSuccess)
        {
            return applied;
        }

        var name = applied.Value.Name;
        var colors = applied.Value.Colors(applied.Value.Worn!.Value);
        var written = await gtk.WriteAsync(name, colors, cancellationToken);
        var preferred = await scheme.PreferAsync(applied.Value.Worn.Value, cancellationToken);
        var dressed = await conf.WriteAsync(name, colors, cancellationToken);
        var reloaded = dressed.IsSuccess ? await kitty.ReloadAsync(cancellationToken) : dressed;
        await beacon.LightAsync("theme", cancellationToken);

        return (written.IsSuccess ? preferred : written, reloaded) switch
        {
            ({ IsSuccess: false } failed, _) => Partly(failed.Error, name, "GTK apps"),
            (_, { IsSuccess: false } failed) => Partly(failed.Error, name, "kitty"),
            _ => applied,
        };
    }

    private static Error Partly(Error error, string name, string what) =>
        error with { Message = $"{name} is applied, but not to {what}: {error.Message}" };
}
