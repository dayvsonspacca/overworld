using System.Text.Json;
using Overworld.Apps.Dye.Palettes;
using Overworld.Apps.Dye.Themes;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Lending;

namespace Overworld.Apps.Dye.Presentation.Lending;

public sealed class DyeLender : ILender
{
    public ILoan? Lend(JsonElement said)
    {
        if (said.ValueKind != JsonValueKind.Object || !said.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        try
        {
            if (said.TryGetProperty("colors", out var colors) && colors.ValueKind == JsonValueKind.Array)
            {
                return said.Deserialize<Palette>(BedrockJson.Options) is { Colors.Count: > 0 } palette ? new PaletteLoan(palette) : null;
            }

            if (said.TryGetProperty("dark", out var dark) && dark.ValueKind == JsonValueKind.Object
                && said.TryGetProperty("light", out var light) && light.ValueKind == JsonValueKind.Object)
            {
                return said.Deserialize<Theme>(BedrockJson.Options) is { } theme ? new ThemeLoan(theme) : null;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
