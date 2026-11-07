using System.ComponentModel;

namespace Overworld.Apps.Dye.Themes;

public sealed record ApplyTheme(
    [property: Description("Name of the theme the shell and the apps should wear, like amethyst; left out, the one worn now.")]
    string? Name = null,
    [property: Description("Whether to wear the theme dark or light; left out, the variant worn now, or dark if none is.")]
    Variant? Variant = null);
