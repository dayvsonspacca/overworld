using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Aside;

public sealed partial class AsideViewModel(string folder, Abilities abilities) : ObservableObject
{
    public TodosViewModel Todos { get; } = new(folder, abilities);

    public SkillsViewModel Skills { get; } = new(folder, abilities);

    public AxiomsViewModel Axioms { get; } = new(folder, abilities);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsTodos), nameof(ShowsSkills), nameof(ShowsAxioms), nameof(ShowsJobs))]
    public partial AsideTab Tab { get; set; }

    public bool ShowsTodos => Tab == AsideTab.Todos;

    public bool ShowsSkills => Tab == AsideTab.Skills;

    public bool ShowsAxioms => Tab == AsideTab.Axioms;

    public bool ShowsJobs => Tab == AsideTab.Jobs;

    [ObservableProperty]
    public partial ConversationViewModel? Conversation { get; set; }

    public Task LoadAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(Todos.LoadAsync(cancellationToken), Skills.LoadAsync(cancellationToken), Axioms.LoadAsync(cancellationToken));

    [RelayCommand]
    private void ShowTodos() => Tab = AsideTab.Todos;

    [RelayCommand]
    private void ShowSkills() => Tab = AsideTab.Skills;

    [RelayCommand]
    private void ShowAxioms() => Tab = AsideTab.Axioms;

    [RelayCommand]
    public void ShowJobs() => Tab = AsideTab.Jobs;

    partial void OnConversationChanged(ConversationViewModel? oldValue, ConversationViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= Watched;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += Watched;
        }

        Settle();
    }

    private void Watched(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConversationViewModel.HasJobs))
        {
            Settle();
        }
    }

    private void Settle()
    {
        if (ShowsJobs && Conversation is not { HasJobs: true })
        {
            Tab = AsideTab.Todos;
        }
    }
}
