using System.Globalization;

namespace Overworld.Allay.Presentation.Conversations;

public sealed class ThoughtEntry(string text, TimeSpan? took = null) : FlowingEntry(text)
{
    public TimeSpan? Took { get; private set; } = took;

    public bool IsDone { get; private set; } = true;

    public string Heading => !IsDone ? "Thinking…" : Took is { } took ? $"Thought for {Spoken(took)}" : "Thought";

    public static ThoughtEntry Thinking(string text) => new(text) { IsDone = false };

    public void Finish(TimeSpan took)
    {
        Took = took;
        IsDone = true;
        OnPropertyChanged(nameof(Took));
        OnPropertyChanged(nameof(IsDone));
        OnPropertyChanged(nameof(Heading));
    }

    internal static string Spoken(TimeSpan took)
    {
        var seconds = Math.Max(1, (int)Math.Round(took.TotalSeconds));

        return seconds < 60
            ? $"{seconds}s"
            : string.Create(CultureInfo.InvariantCulture, $"{seconds / 60}m {seconds % 60:00}s");
    }
}
