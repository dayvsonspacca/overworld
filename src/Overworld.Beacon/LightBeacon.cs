using System.ComponentModel;

namespace Overworld.Beacon;

public sealed record LightBeacon(
    [property: Description("Name of what changed, like theme or pins; whoever watches for it reacts.")]
    string Signal);
