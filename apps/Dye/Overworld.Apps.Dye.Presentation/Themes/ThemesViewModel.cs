using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Dye.Themes;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Dye.Presentation.Themes;

public sealed partial class ThemesViewModel(
    Operation<ListThemes, IReadOnlyList<Theme>> list,
    Operation<ApplyTheme, Theme> apply) : ObservableObject
{
    private IReadOnlyList<Theme> _listed = [];

    public ObservableCollection<ThemeLook> Themes { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Groups), nameof(Saving), nameof(CanApply), nameof(Applying))]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial ThemeLook? Selected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsDark), nameof(ShowsLight))]
    public partial Variant Shown { get; set; }

    public bool ShowsDark => Shown == Variant.Dark;

    public bool ShowsLight => Shown == Variant.Light;

    public IReadOnlyList<RoleGroup> Groups => Selected is null ? [] : RoleGroup.Of(Selected.Theme.Colors(Shown));

    public string Saving =>
        $"overworld:dye themes save --name ocean --from {Selected?.Name ?? "amethyst"} --dark \"accent=#3A7BD5\" --light \"accent=#2F62B0\"";

    public bool CanApply => Selected is not null && Selected.Theme.Worn != Shown;

    public string Applying =>
        !CanApply ? "Applied"
        : Selected!.Active ? $"Apply {Shown.ToString().ToLowerInvariant()}"
        : "Apply";

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public Task LoadAsync(CancellationToken cancellationToken) => LoadAsync(Selected?.Name, cancellationToken);

    [RelayCommand]
    private void Show(Variant variant) => Shown = variant;

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync(CancellationToken cancellationToken)
    {
        var applied = await apply.ExecuteAsync(new ApplyTheme(Selected!.Name, Shown), cancellationToken);

        if (!applied.IsSuccess)
        {
            Problem = applied.Error.Message;

            return;
        }

        await LoadAsync(applied.Value.Name, cancellationToken);
    }

    partial void OnShownChanged(Variant value) => Dress(Selected?.Name);

    private async Task LoadAsync(string? select, CancellationToken cancellationToken)
    {
        var listed = await list.ExecuteAsync(new ListThemes(), cancellationToken);

        if (!listed.IsSuccess)
        {
            Problem = listed.Error.Message;

            return;
        }

        Problem = null;
        _listed = listed.Value;

        if (_listed.FirstOrDefault(theme => theme.Active)?.Worn is { } worn && worn != Shown)
        {
            Shown = worn;
        }

        Dress(select);
    }

    private void Dress(string? select)
    {
        Themes.Clear();

        foreach (var theme in _listed)
        {
            Themes.Add(ThemeLook.Of(theme, Shown));
        }

        Selected = Themes.FirstOrDefault(theme => theme.Name == select)
            ?? Themes.FirstOrDefault(theme => theme.Active)
            ?? Themes.FirstOrDefault();
    }
}
