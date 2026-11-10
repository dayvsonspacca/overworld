namespace Overworld.Allay.Presentation.Conversations;

public static class Hidden
{
    public static IReadOnlyList<(Type Type, string Property)> Properties { get; } =
    [
        (typeof(Timeline), nameof(Timeline.Entries)),
        (typeof(SpeechEntry), nameof(SpeechEntry.Blocks)),
        (typeof(PromptEntry), nameof(PromptEntry.Prompt)),
        (typeof(PictureView), nameof(PictureView.Picture)),
        (typeof(PlanEntry), nameof(PlanEntry.Steps)),
    ];
}
