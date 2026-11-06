using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;

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
}
