using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Allay.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Tests.Conversations;

public sealed class RequestsTest
{
    private readonly List<(string Request, Decision Decision)> _decided = [];

    private readonly Timeline _timeline;

    public RequestsTest() => _timeline = new Timeline((request, decision) => _decided.Add((request, decision)));

    private static readonly Question Commit = new("How should it commit?", "Commit", false, [new Option("One", "A single commit"), new Option("Two", "")]);

    private static readonly Question Checks = new("Which checks?", "Checks", true, [new Option("Build", ""), new Option("Tests", "")]);

    [Fact]
    public void AnswersOnlyOnceEveryQuestionIsAnswered()
    {
        _timeline.Add(new Questions("q-1", [Commit, Checks]));
        var questions = Assert.IsType<QuestionsEntry>(_timeline.Entries[0]);

        questions.Asked[0].Options[0].IsPicked = true;
        questions.Asked[0].Options[1].IsPicked = true;
        Assert.False(questions.SendAnswersCommand.CanExecute(null));

        questions.Asked[1].Options[0].IsPicked = true;
        questions.Asked[1].Options[1].IsPicked = true;
        questions.Asked[1].Written = " lint ";
        questions.SendAnswersCommand.Execute(null);

        var (request, decision) = Assert.Single(_decided);
        Assert.Equal("q-1", request);
        var given = Assert.IsType<Answers>(decision).Given;
        Assert.Equal(["Two"], given[0].Picked.Select(option => option.Label));
        Assert.Equal(["Build", "Tests"], given[1].Picked.Select(option => option.Label));
        Assert.Equal("lint", given[1].Written);
        Assert.Equal("Answered", questions.Answer);
        Assert.False(questions.SendAnswersCommand.CanExecute(null));
    }

    [Fact]
    public void GoesThroughTheQuestionsOneAtATime()
    {
        _timeline.Add(new Questions("q-1", [Commit, Checks]));
        var questions = Assert.IsType<QuestionsEntry>(_timeline.Entries[0]);

        Assert.True(questions.HasSteps);
        Assert.False(questions.NextCommand.CanExecute(null));
        Assert.False(questions.BackCommand.CanExecute(null));

        questions.Asked[0].Options[0].IsPicked = true;
        questions.NextCommand.Execute(null);

        Assert.Equal(1, questions.Step);
        Assert.True(questions.IsLast);
        Assert.False(questions.NextCommand.CanExecute(null));

        questions.BackCommand.Execute(null);
        questions.GoCommand.Execute(1);

        Assert.Equal(1, questions.Step);
    }

    [Fact]
    public void PicksTheRecommendedOptionAndNamesItWithoutTheMark()
    {
        _timeline.Add(new Questions("q-1", [new Question("Which?", "Pick", false, [new Option("Plain", ""), new Option("Safe (Recommended)", "")])]));
        var question = Assert.IsType<QuestionsEntry>(_timeline.Entries[0]).Asked[0];

        Assert.Equal("Safe", question.Options[1].Label);
        Assert.True(question.Options[1].IsRecommended);
        Assert.True(question.Options[1].IsPicked);
        Assert.Equal("Safe (Recommended)", question.Answer().Picked[0].Label);
    }

    [Fact]
    public void WritingAnAnswerToOneChoiceUnpicksTheOptions()
    {
        _timeline.Add(new Questions("q-1", [Commit]));
        var question = Assert.IsType<QuestionsEntry>(_timeline.Entries[0]).Asked[0];
        question.Options[0].IsPicked = true;

        question.IsWriting = true;
        question.Written = "Three";

        Assert.Empty(question.Answer().Picked);
        Assert.Equal("Three", question.Answer().Written);

        question.Options[1].IsPicked = true;

        Assert.False(question.IsWriting);
        Assert.Null(question.Answer().Written);
    }

    [Fact]
    public void SkipsQuestions()
    {
        _timeline.Add(new Questions("q-1", [Commit]));

        Assert.IsType<QuestionsEntry>(_timeline.Entries[0]).SkipCommand.Execute(null);

        Assert.IsType<Skip>(Assert.Single(_decided).Decision);
    }

    [Fact]
    public void ClosesARequestTheAgentWithdrew()
    {
        _timeline.Add(new Permission("p-1", ActKind.Change, "src/A.cs", null, []));
        _timeline.Add(new Withdrawn("p-1"));
        var permission = Assert.IsType<PermissionEntry>(_timeline.Entries[0]);

        permission.AllowOnceCommand.Execute(null);

        Assert.Equal("Withdrawn", permission.Answer);
        Assert.False(permission.CanRemember);
        Assert.Equal("Change a file?", permission.Asks);
        Assert.Empty(_decided);
    }

    [Fact]
    public void SettlesARequestDecidedOnTheDesktop()
    {
        _timeline.Add(new Permission("p-1", ActKind.Command, "just test", null, []));

        var entry = _timeline.Request("p-1")!;
        entry.Settle(new AllowOnce());
        entry.Settle(new Deny());

        Assert.IsType<AllowOnce>(Assert.Single(_decided).Decision);
        Assert.Equal("Allowed once", entry.Answer);
    }
}
