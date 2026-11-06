using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Presentation.Markdown;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed partial class SummaryProposalEntry(string? current, string proposed) : ProposalEntry
{
    public string Asking { get; } = string.IsNullOrWhiteSpace(current) ? "A summary for the project" : "A new summary for the project";

    public IReadOnlyList<Block> Blocks { get; } = Prose.Parse(proposed);

    public IReadOnlyList<SummaryLine> Lines { get; } = SummaryLine.Between(current, proposed);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsPreview))]
    public partial bool ShowsChanges { get; private set; }

    public bool ShowsPreview => !ShowsChanges;

    [RelayCommand]
    private void ShowPreview() => ShowsChanges = false;

    [RelayCommand]
    private void ShowChanges() => ShowsChanges = true;
}
