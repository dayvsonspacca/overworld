using System.Text.RegularExpressions;
using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

[Operation("themes-save", "Saves a new theme made from another with some colors changed, in Overworld's themes kept with its config, and signals that the themes changed.")]
public sealed partial class SaveThemeOp(ThemeStore store, BeaconClient beacon) : IOperation<SaveTheme, Theme>
{
    public async Task<Result<Theme>> ExecuteAsync(SaveTheme input, CancellationToken cancellationToken)
    {
        Dictionary<string, string> colors = [];

        foreach (var change in input.Colors ?? [])
        {
            var (role, hex) = change.Split('=', 2, StringSplitOptions.TrimEntries) is [var left, var right]
                ? (left, right)
                : ("", "");

            if (role.Length == 0 || !Hex().IsMatch(hex))
            {
                return new Error("theme.color_invalid", $"'{change}' is not a color change like accent=#3A7BD5.");
            }

            if (!colors.TryAdd(role, hex.ToUpperInvariant()))
            {
                return new Error("theme.color_repeated", $"'{role}' is changed more than once.");
            }
        }

        var saved = await store.SaveAsync(input.Name.Trim(), input.From.Trim(), colors, cancellationToken);

        if (saved.IsSuccess)
        {
            await beacon.LightAsync("themes", cancellationToken);
        }

        return saved;
    }

    [GeneratedRegex("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")]
    private static partial Regex Hex();
}
