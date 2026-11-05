using System.ComponentModel;

namespace Overworld.Daemon.Pins;

internal sealed record ListPins(
    [property: Description("Where the pins are: menu or taskbar.")]
    string Place);
