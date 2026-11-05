using System.ComponentModel;

namespace Overworld.Seed;

public sealed record Install(
    [property: Description("Theme for the shell and the apps, by name, like amethyst.")]
    string Theme = "amethyst");
