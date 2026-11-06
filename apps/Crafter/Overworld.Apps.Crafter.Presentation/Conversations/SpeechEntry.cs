using Overworld.Apps.Crafter.Presentation.Markdown;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class SpeechEntry(string text) : FlowingEntry(text)
{
    private IReadOnlyList<Block>? _blocks;

    public IReadOnlyList<Block> Blocks => _blocks ??= Prose.Parse(Text);

    protected override void Grew()
    {
        _blocks = null;
        OnPropertyChanged(nameof(Blocks));
    }
}
