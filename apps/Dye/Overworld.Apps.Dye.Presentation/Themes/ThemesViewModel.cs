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
    public ObservableCollection<ThemeLook> Themes { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Groups), nameof(Saving), nameof(CanApply))]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial ThemeLook? Selected { get; set; }

    public IReadOnlyList<RoleGroup> Groups => Selected is null ? [] : RoleGroup.Of(Selected.Theme);

    public string Saving =>
        $"overworld:dye themes save --name ocean --from {Selected?.Name ?? "amethyst"} --colors \"accent=#3A7BD5\"";

    public bool CanApply => Selected is { Active: false };

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public Task LoadAsync(CancellationToken cancellationToken) => LoadAsync(Selected?.Name, cancellationToken);

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync(CancellationToken cancellationToken)
    {
        var applied = await apply.ExecuteAsync(new ApplyTheme(Selected!.Name), cancellationToken);

        if (!applied.IsSuccess)
        {
            Problem = applied.Error.Message;

            return;
        }

        await LoadAsync(applied.Value.Name, cancellationToken);
    }

    private async Task LoadAsync(string? select, CancellationToken cancellationToken)
    {
        var listed = await list.ExecuteAsync(new ListThemes(), cancellationToken);

        if (!listed.IsSuccess)
        {
            Problem = listed.Error.Message;

            return;
        }

        Problem = null;
        Themes.Clear();

        foreach (var theme in listed.Value)
        {
            Themes.Add(ThemeLook.Of(theme));
        }

        Selected = Themes.FirstOrDefault(theme => theme.Name == select)
            ?? Themes.FirstOrDefault(theme => theme.Active)
            ?? Themes.FirstOrDefault();
    }
}
