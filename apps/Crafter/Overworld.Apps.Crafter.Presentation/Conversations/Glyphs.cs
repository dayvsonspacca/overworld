using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public static class Glyphs
{
    public static string Of(ActKind kind) => kind switch
    {
        ActKind.Command => "$",
        ActKind.Read => "≡",
        ActKind.Change => "✎",
        ActKind.Create => "+",
        ActKind.Search => "⌕",
        ActKind.Browse => "◎",
        ActKind.Delegate => "⇢",
        ActKind.Tool => "⚙",
        _ => "•",
    };
}
