using System.ComponentModel;

namespace Overworld.Daemon.Pins;

internal sealed record RemovePin(
    [property: Description("Where to unpin the app from: menu or taskbar.")]
    string Place,
    [property: Description("Desktop entry id of the app, like firefox.")]
    string App);
