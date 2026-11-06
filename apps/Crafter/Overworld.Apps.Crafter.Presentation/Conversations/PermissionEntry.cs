using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed partial class PermissionEntry(Permission permission, Action<string, Decision> decide) : RequestEntry(permission.Id, decide)
{
    public string Glyph { get; } = Glyphs.Of(permission.Kind);

    public string Asks { get; } = permission.Kind switch
    {
        ActKind.Command => "Run a command?",
        ActKind.Change => "Change a file?",
        ActKind.Create => "Create a file?",
        ActKind.Read => "Read a file?",
        ActKind.Browse => "Open a page?",
        _ => "Go ahead?",
    };

    public string Subject { get; } = permission.Subject;

    public string? Reason { get; } = permission.Reason;

    public bool HasReason => Reason is { Length: > 0 };

    public string? Remembers { get; } = permission.Remembers.Count > 0 ? string.Join(", ", permission.Remembers.Select(allowance => allowance.What)) : null;

    public bool CanRemember => Remembers is not null;

    [RelayCommand]
    private void AllowOnce() => Decide(new AllowOnce(), "Allowed once");

    [RelayCommand]
    private void AllowAlways() => Decide(new AllowAlways(), $"Always allowed: {Remembers}");

    [RelayCommand]
    private void Deny() => Decide(new Deny(), "Denied");
}
