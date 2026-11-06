using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Crafter.Axioms;

namespace Overworld.Apps.Crafter.Presentation.Aside;

public sealed partial class AxiomsViewModel(string folder, Abilities abilities) : ObservableObject
{
    public ObservableCollection<string> Axioms { get; } = [];

    public bool HasAxioms => Axioms.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewAxiom { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Problem { get; set; }

    public event EventHandler? Changed;

    public async Task LoadAsync(CancellationToken cancellationToken) =>
        Show(await abilities.ListAxioms.ExecuteAsync(new ListAxioms(folder), cancellationToken));

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        var added = await abilities.AddAxiom.ExecuteAsync(new AddAxiom(folder, NewAxiom), CancellationToken.None);

        if (Show(added))
        {
            NewAxiom = string.Empty;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(string axiom)
    {
        if (Show(await abilities.RemoveAxiom.ExecuteAsync(new RemoveAxiom(folder, axiom), CancellationToken.None)))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool CanAdd() => NewAxiom.Trim().Length > 0;

    private bool Show(Bedrock.Results.Result<IReadOnlyList<string>> axioms)
    {
        if (!axioms.IsSuccess)
        {
            Problem = axioms.Error.Message;

            return false;
        }

        Problem = null;
        Axioms.Clear();

        foreach (var axiom in axioms.Value)
        {
            Axioms.Add(axiom);
        }

        OnPropertyChanged(nameof(HasAxioms));

        return true;
    }
}
