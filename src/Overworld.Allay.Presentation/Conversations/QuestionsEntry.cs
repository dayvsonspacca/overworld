using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Conversations;

namespace Overworld.Allay.Presentation.Conversations;

public sealed partial class QuestionsEntry : RequestEntry
{
    public QuestionsEntry(Questions questions, Action<string, Decision> decide)
        : base(questions.Id, decide)
    {
        Asked = [.. questions.Asked.Select(question => new QuestionView(question, Changed))];
    }

    public IReadOnlyList<QuestionView> Asked { get; }

    public bool HasSteps => Asked.Count > 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLast))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand), nameof(BackCommand))]
    public partial int Step { get; private set; }

    public bool IsLast => Step == Asked.Count - 1;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private void SendAnswers() => Decide(new Answers([.. Asked.Select(question => question.Answer())]), "Answered");

    [RelayCommand]
    private void Skip() => Decide(new Skip(), "Skipped");

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next() => Step++;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back() => Step--;

    [RelayCommand]
    private void Go(int step)
    {
        if (IsOpen && step >= 0 && step < Asked.Count)
        {
            Step = step;
        }
    }

    private bool CanSend() => IsOpen && Asked.All(question => question.IsAnswered);

    private bool CanGoNext() => IsOpen && !IsLast && Asked[Step].IsAnswered;

    private bool CanGoBack() => IsOpen && Step > 0;

    private void Changed()
    {
        SendAnswersCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }
}
