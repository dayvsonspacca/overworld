using Overworld.Allay.Conversations;
using Overworld.Allay.Bells;
using Overworld.Bell;
using AllayQuestion = Overworld.Allay.Conversations.Question;
using AllayQuestions = Overworld.Allay.Conversations.Questions;
using BellQuestions = Overworld.Bell.Questions;

namespace Overworld.Allay.Tests.Bells;

public sealed class RequestBellTest
{
    private static readonly Permission Run = new("p", ActKind.Command, "just test", "To check it", [new Allowance("just", Reach.Folder)]);

    private static readonly AllayQuestions Asked = new(
        "q",
        [
            new AllayQuestion("Where?", "Storage", false, [new Option("Chest", "A file"), new Option("Memory", "Gone on restart")]),
            new AllayQuestion("What?", "Scope", true, [new Option("Questions", ""), new Option("Permissions", "")]),
        ]);

    [Fact]
    public void RingsAPermissionAsButtons()
    {
        var ring = RequestBell.Ring(Run, "crafter", "overworld · Bell", "#7FC79A", quiet: false);

        var buttons = Assert.IsType<Buttons>(ring.Ask);
        Assert.Equal("crafter", ring.From);
        Assert.Equal("Run a command?", ring.Title);
        Assert.Equal("just test", buttons.Subject);
        Assert.Equal(["Deny", "Always allow", "Allow once"], buttons.Offered.Select(button => button.Label));
    }

    [Fact]
    public void OffersAlwaysOnlyWhenThereIsSomethingToRemember()
    {
        var ring = RequestBell.Ring(Run with { Remembers = [] }, "crafter", "overworld", null, quiet: false);

        Assert.Equal(["Deny", "Allow once"], Assert.IsType<Buttons>(ring.Ask).Offered.Select(button => button.Label));
    }

    [Fact]
    public void RingsQuestionsWithTheirChoices()
    {
        var ring = RequestBell.Ring(Asked, "crafter", "overworld", null, quiet: true);

        var questions = Assert.IsType<BellQuestions>(ring.Ask);
        Assert.True(ring.Quiet);
        Assert.Equal("Storage", questions.Asked[0].Header);
        Assert.Equal("Gone on restart", questions.Asked[0].Choices[1].Detail);
        Assert.True(questions.Asked[1].Several);
    }

    [Fact]
    public void TurnsAPressedButtonIntoItsDecision()
    {
        var buttons = Assert.IsType<Buttons>(RequestBell.Ring(Run, "crafter", "overworld", null, quiet: false).Ask);
        var key = (string label) => buttons.Offered.Single(button => button.Label == label).Key;

        Assert.IsType<AllowOnce>(RequestBell.Decision(Run, new Pressed(key("Allow once"))));
        Assert.IsType<AllowAlways>(RequestBell.Decision(Run, new Pressed(key("Always allow"))));
        Assert.IsType<Deny>(RequestBell.Decision(Run, new Pressed(key("Deny"))));
    }

    [Fact]
    public void TurnsAnswersIntoTheOptionsPickedAndWordsWritten()
    {
        var decision = RequestBell.Decision(Asked, new Answered([new Picked(["Memory"]), new Picked([], "Both, later")]));

        var answers = Assert.IsType<Answers>(decision);
        Assert.Equal([new Option("Memory", "Gone on restart")], answers.Given[0].Picked);
        Assert.Empty(answers.Given[1].Picked);
        Assert.Equal("Both, later", answers.Given[1].Written);
    }

    [Fact]
    public void ASkippedNoticeSkips()
    {
        Assert.IsType<Skip>(RequestBell.Decision(Asked, new Skipped()));
    }
}
