using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Dye.Presentation.Themes;
using Overworld.Apps.Dye.Themes;
using Overworld.Bedrock.Lending;

namespace Overworld.Apps.Dye.Presentation.Lending;

public sealed partial class ThemeLoan(Theme theme) : ObservableObject, ILoan
{
    private readonly Variant _variant = theme.Worn ?? Variant.Dark;

    public string Key => $"dye:theme:{theme.Name}";

    public string Source => "Dye";

    public string Title => theme.Active
        ? $"{theme.Name} · {_variant.ToString().ToLowerInvariant()} · worn"
        : $"{theme.Name} · {_variant.ToString().ToLowerInvariant()}";

    public IReadOnlyList<RoleGroup> Groups => RoleGroup.Of(theme.Colors(_variant));

    [ObservableProperty]
    public partial bool IsLive { get; set; }
}
