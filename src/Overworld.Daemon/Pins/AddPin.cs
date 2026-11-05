using System.ComponentModel;

namespace Overworld.Daemon.Pins;

internal sealed record AddPin(
    [property: Description("Where to pin the app: menu or taskbar.")]
    string Place,
    [property: Description("Desktop entry id of the app, like firefox.")]
    string App);
