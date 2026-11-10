using System.Text;

namespace Overworld.Allay.Presentation.Conversations;

public abstract class FlowingEntry(string text) : Entry
{
    private readonly StringBuilder _text = new(text);

    public string Text => _text.ToString();

    public void Append(string more)
    {
        _text.Append(more);
        OnPropertyChanged(nameof(Text));
        Grew();
    }

    protected virtual void Grew()
    {
    }
}
