using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed partial class ActEntry(Act act) : Entry
{
    private const int LongestOutput = 12;

    public string Id { get; } = act.Id;

    public ActKind Kind { get; } = act.Kind;

    public string Subject { get; } = act.Subject;

    public string Glyph { get; } = act.Kind switch
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

    public string? Added { get; } = act.Changes is { Added: > 0 } changes ? $"+{changes.Added}" : null;

    public string? Removed { get; } = act.Changes is { Removed: > 0 } changes ? $"−{changes.Removed}" : null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning), nameof(HasFailed), nameof(Verdict), nameof(ShowsOutput))]
    public partial ActState State { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutput), nameof(ShowsOutput), nameof(Shown))]
    public partial string Output { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsOutput))]
    public partial bool IsOpen { get; set; }

    public bool IsRunning => State == ActState.Running;

    public bool HasFailed => State == ActState.Failed;

    public bool HasOutput => Output.Length > 0;

    public bool ShowsOutput => HasOutput && IsOpen;

    public string? Verdict => State switch
    {
        ActState.Failed => "failed",
        ActState.Running => "…",
        _ => null,
    };

    public string Shown
    {
        get
        {
            var lines = Output.TrimEnd().Split('\n');

            return lines.Length <= LongestOutput
                ? string.Join('\n', lines)
                : string.Join('\n', lines[..LongestOutput]) + $"\n… {lines.Length - LongestOutput} more lines";
        }
    }

    public void Settle(ActResult result)
    {
        Output = result.Output;
        State = result.Succeeded ? ActState.Succeeded : ActState.Failed;
        IsOpen = !result.Succeeded;
    }

    [RelayCommand]
    private void Toggle() => IsOpen = !IsOpen;
}
