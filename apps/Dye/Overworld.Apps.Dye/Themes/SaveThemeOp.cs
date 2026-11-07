using System.Text.RegularExpressions;
using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Themes;

[Operation("themes-save", "Saves a new theme made from another, dark and light, with some colors changed, in Overworld's themes kept with its config, and signals that the themes changed.")]
public sealed partial class SaveThemeOp(ThemeStore store, BeaconClient beacon) : IOperation<SaveTheme, Theme>
{
    public async Task<Result<Theme>> ExecuteAsync(SaveTheme input, CancellationToken cancellationToken)
    {
        Dictionary<Variant, IReadOnlyDictionary<string, string>> changes = [];

        foreach (var (variant, list) in new[] { (Variant.Dark, input.Dark), (Variant.Light, input.Light) })
        {
            var colors = Parse(list ?? []);

            if (!colors.IsSuccess)
            {
                return colors.Error;
            }

            changes[variant] = colors.Value;
        }

        var saved = await store.SaveAsync(input.Name.Trim(), input.From.Trim(), changes, cancellationToken);

        if (saved.IsSuccess)
        {
            await beacon.LightAsync("themes", cancellationToken);
        }

        return saved;
    }

    private static Result<IReadOnlyDictionary<string, string>> Parse(IReadOnlyList<string> changes)
    {
        Dictionary<string, string> colors = [];

        foreach (var change in changes)
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

        return colors;
    }

    [GeneratedRegex("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")]
    private static partial Regex Hex();
}
