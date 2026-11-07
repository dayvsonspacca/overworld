using System.ComponentModel;

namespace Overworld.Bell;

public sealed record CollectNotices(
    [property: Description("App id that rang the notices, like crafter.")]
    string From);
