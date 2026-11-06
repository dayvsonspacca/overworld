using CommunityToolkit.Mvvm.Input;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed partial class QuestionsEntry : RequestEntry
{
    public QuestionsEntry(Questions questions, Action<string, Decision> decide)
        : base(questions.Id, decide)
    {
        Asked = [.. questions.Asked.Select(question => new QuestionView(question, () => SendAnswersCommand.NotifyCanExecuteChanged()))];
    }

    public IReadOnlyList<QuestionView> Asked { get; }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private void SendAnswers() => Decide(new Answers([.. Asked.Select(question => question.Answer())]), "Answered");

    [RelayCommand]
    private void Skip() => Decide(new Skip(), "Skipped");

    private bool CanSend() => IsOpen && Asked.All(question => question.IsAnswered);
}
