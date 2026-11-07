using System.ComponentModel;

namespace Overworld.Seed;

public sealed record Install(
    [property: Description("Theme for the shell and the apps, by name, like amethyst. Without it, the theme worn now stays, or amethyst on a new machine; the variant worn, dark or light, stays either way.")]
    string? Theme = null);
