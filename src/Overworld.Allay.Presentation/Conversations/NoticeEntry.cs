using Overworld.Allay.Conversations;

namespace Overworld.Allay.Presentation.Conversations;

public sealed class NoticeEntry(Notice notice) : Entry
{
    public NoticeKind Kind { get; } = notice.Kind;

    public string Text { get; } = notice.Text;
}
