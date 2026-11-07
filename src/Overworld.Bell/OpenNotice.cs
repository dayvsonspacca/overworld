using System.ComponentModel;

namespace Overworld.Bell;

public sealed record OpenNotice(
    [property: Description("Id of the notice, as bell-list gives it.")]
    string Id);
