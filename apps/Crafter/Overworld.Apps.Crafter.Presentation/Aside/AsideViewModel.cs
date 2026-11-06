using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Overworld.Apps.Crafter.Presentation.Aside;

public sealed partial class AsideViewModel(string folder, Abilities abilities) : ObservableObject
{
    public TodosViewModel Todos { get; } = new(folder, abilities);

    public SkillsViewModel Skills { get; } = new(folder, abilities);

    public AxiomsViewModel Axioms { get; } = new(folder, abilities);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsTodos), nameof(ShowsSkills), nameof(ShowsAxioms))]
    public partial AsideTab Tab { get; set; }

    public bool ShowsTodos => Tab == AsideTab.Todos;

    public bool ShowsSkills => Tab == AsideTab.Skills;

    public bool ShowsAxioms => Tab == AsideTab.Axioms;

    public Task LoadAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(Todos.LoadAsync(cancellationToken), Skills.LoadAsync(cancellationToken), Axioms.LoadAsync(cancellationToken));

    [RelayCommand]
    private void ShowTodos() => Tab = AsideTab.Todos;

    [RelayCommand]
    private void ShowSkills() => Tab = AsideTab.Skills;

    [RelayCommand]
    private void ShowAxioms() => Tab = AsideTab.Axioms;
}
