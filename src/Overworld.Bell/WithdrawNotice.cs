using System.ComponentModel;

namespace Overworld.Bell;

public sealed record WithdrawNotice(
    [property: Description("Id of the notice, as bell-ring gave it.")]
    string Id);
