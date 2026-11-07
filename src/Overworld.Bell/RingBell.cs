using System.ComponentModel;

namespace Overworld.Bell;

public sealed record RingBell(
    [property: Description("App id that rings, like crafter; it collects the replies.")]
    string From,
    [property: Description("What the notice says, in a line.")]
    string Title,
    [property: Description("More about it, under the title.")]
    string? Body = null,
    [property: Description("Where it comes from inside the app, like a project and a chat.")]
    string? Context = null,
    [property: Description("Hex color of the agent behind it, shown as a dot.")]
    string? Color = null,
    [property: Description("What it asks: questions with choices, or buttons; none for a notice that only tells.")]
    Ask? Ask = null,
    [property: Description("Keep it in the control center without a toast, when it is already on screen.")]
    bool Quiet = false);
