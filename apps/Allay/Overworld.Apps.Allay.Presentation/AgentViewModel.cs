using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Agents;
using Overworld.Allay.Hiring;
using Overworld.Allay.Settings;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Allay.Presentation;

public sealed partial class AgentViewModel : ObservableObject
{
    private static readonly ModelOption Default = new(Crew.DefaultModel, "Default model");

    private readonly Operation<CheckAgent, Readiness> _check;
    private readonly Operation<HireAgent, AgentCard> _hire;
    private readonly Operation<ConfigureAgent, AgentCard> _configure;
    private AgentCard _card;
    private bool _showing;

    public AgentViewModel(
        AgentCard card,
        Operation<CheckAgent, Readiness> check,
        Operation<HireAgent, AgentCard> hire,
        Operation<ConfigureAgent, AgentCard> configure)
    {
        (_card, _check, _hire, _configure) = (card, check, hire, configure);
        Show(card);
    }

    public string Id => _card.Id;

    public string Name => _card.Name;

    public string Summary => _card.Summary;

    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(word => char.ToUpperInvariant(word[0])));

    public ObservableCollection<ModelOption> Models { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Standing))]
    [NotifyCanExecuteChangedFor(nameof(HireCommand), nameof(LetGoCommand))]
    public partial bool IsHired { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChecking), nameof(IsReady), nameof(IsBlocked), nameof(State), nameof(Detail), nameof(Remedy), nameof(Standing))]
    [NotifyCanExecuteChangedFor(nameof(HireCommand))]
    public partial Readiness? Readiness { get; private set; }

    public bool IsChecking => Readiness is null;

    public bool IsReady => Readiness is Ready;

    public bool IsBlocked => Readiness is Blocked;

    public string State => Readiness?.State ?? "Checking…";

    public string? Detail => Readiness?.Detail;

    public string? Remedy => (Readiness as Blocked)?.Remedy;

    public string Standing => $"{(IsHired ? "Hired" : "Not hired")} · {State}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuto), nameof(IsEdits), nameof(IsPlan), nameof(ModeHint))]
    public partial Mode Mode { get; private set; }

    public bool IsAuto => Mode == Mode.Auto;

    public bool IsEdits => Mode == Mode.Edits;

    public bool IsPlan => Mode == Mode.Plan;

    public string ModeHint => Mode switch
    {
        Mode.Edits => "Changes files, but asks before anything else.",
        Mode.Plan => "Only plans.",
        _ => "Does what it needs.",
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMax))]
    public partial Effort Effort { get; private set; }

    public bool IsMax => Effort == Effort.Max;

    [ObservableProperty]
    public partial ModelOption? Model { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HireCommand), nameof(LetGoCommand))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string? Problem { get; private set; }

    [RelayCommand]
    public async Task CheckAsync(CancellationToken cancellationToken)
    {
        Readiness = null;
        var checkedOut = await _check.ExecuteAsync(new CheckAgent(Id), cancellationToken);
        Readiness = checkedOut.IsSuccess ? checkedOut.Value : new Blocked("Not checked", checkedOut.Error.Message);
    }

    [RelayCommand(CanExecute = nameof(CanHire))]
    private Task HireAsync() => ApplyAsync(() => _hire.ExecuteAsync(new HireAgent(Id), CancellationToken.None));

    [RelayCommand(CanExecute = nameof(CanLetGo))]
    private Task LetGoAsync() => ApplyAsync(() => _hire.ExecuteAsync(new HireAgent(Id, Hired: false), CancellationToken.None));

    [RelayCommand]
    private Task PickModeAsync(Mode mode) => ApplyAsync(() => _configure.ExecuteAsync(new ConfigureAgent(Id, Mode: mode), CancellationToken.None));

    [RelayCommand]
    private Task PickEffortAsync(Effort effort) => ApplyAsync(() => _configure.ExecuteAsync(new ConfigureAgent(Id, Effort: effort), CancellationToken.None));

    partial void OnModelChanged(ModelOption? value)
    {
        if (!_showing && value is not null)
        {
            _ = ApplyAsync(() => _configure.ExecuteAsync(new ConfigureAgent(Id, Model: value.Id), CancellationToken.None));
        }
    }

    private bool CanHire() => IsReady && !IsHired && !IsBusy;

    private bool CanLetGo() => IsHired && !IsBusy;

    private async Task ApplyAsync(Func<Task<Result<AgentCard>>> change)
    {
        IsBusy = true;

        try
        {
            var changed = await change();
            Problem = changed.IsSuccess ? null : changed.Error.Message;
            Show(changed.IsSuccess ? changed.Value : _card);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Show(AgentCard card)
    {
        _showing = true;

        try
        {
            _card = card;
            IsHired = card.Hired;
            Mode = card.Setup.Mode;
            Effort = card.Setup.Effort;

            var chosen = card.Setup.Model ?? Crew.DefaultModel;
            Models.Clear();
            Models.Add(Default);

            foreach (var option in card.Models)
            {
                Models.Add(option);
            }

            if (Models.All(option => option.Id != chosen))
            {
                Models.Add(new ModelOption(chosen, chosen));
            }

            Model = Models.First(option => option.Id == chosen);
        }
        finally
        {
            _showing = false;
        }
    }
}
