using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Apps.Crafter.Presentation.Markdown;

namespace Overworld.Apps.Crafter.Presentation.Tests.Conversations;

public sealed class TimelineTest
{
    private readonly Timeline _timeline = new();

    [Fact]
    public void JoinsWhatFlowsInPiecesAndSplitsWhenTheKindChanges()
    {
        _timeline.Add(new Thought("Look"));
        _timeline.Add(new Thought("ing."));
        _timeline.Add(new Speech("Hello "));
        _timeline.Add(new Speech("**there**"));

        Assert.Equal("Looking.", Assert.IsType<ThoughtEntry>(_timeline.Entries[0]).Text);
        var speech = Assert.IsType<SpeechEntry>(_timeline.Entries[1]);
        Assert.Equal("Hello **there**", speech.Text);
        Assert.Contains(Assert.IsType<Paragraph>(Assert.Single(speech.Blocks)).Fragments, fragment => fragment is { Text: "there", Look: Look.Bold });
    }

    [Fact]
    public void SettlesAnActWithItsResultAndOpensItOnlyWhenItFailed()
    {
        _timeline.Add(new Act("1", ActKind.Command, "dotnet test"));
        _timeline.Add(new Act("2", ActKind.Change, "src/A.cs", new Changes(3, 1)));
        var command = Assert.IsType<ActEntry>(_timeline.Entries[0]);
        Assert.True(command.IsRunning);

        _timeline.Add(new ActResult("1", false, "Failed RefusesTwoOutcomes"));
        _timeline.Add(new ActResult("2", true, "ok"));

        Assert.Equal(("$", "failed", true), (command.Glyph, command.Verdict, command.ShowsOutput));
        var change = Assert.IsType<ActEntry>(_timeline.Entries[1]);
        Assert.Equal(("+3", "−1", false), (change.Added, change.Removed, change.ShowsOutput));

        change.ToggleCommand.Execute(null);

        Assert.True(change.ShowsOutput);
    }

    [Fact]
    public void CutsALongOutput()
    {
        _timeline.Add(new Act("1", ActKind.Command, "seq 20"));
        _timeline.Add(new ActResult("1", true, string.Join('\n', Enumerable.Range(1, 20))));

        var shown = Assert.IsType<ActEntry>(_timeline.Entries[0]).Shown.Split('\n');

        Assert.Equal(13, shown.Length);
        Assert.Equal("… 8 more lines", shown[^1]);
    }

    [Fact]
    public void KeepsOnePlanPerReplyUpdatedWhereItFirstShowed()
    {
        _timeline.Add(new Plan([new PlanStep("Find", StepState.Doing), new PlanStep("Fix", StepState.Pending)]));
        _timeline.Add(new Speech("Found it."));
        _timeline.Add(new Plan([new PlanStep("Find", StepState.Done), new PlanStep("Fix", StepState.Doing)]));

        var plan = Assert.IsType<PlanEntry>(_timeline.Entries[0]);
        Assert.Equal(2, _timeline.Entries.Count);
        Assert.Equal("1/2", plan.Progress);

        _timeline.Add(new Prompt("Again", [], []));
        _timeline.Add(new Plan([new PlanStep("Redo", StepState.Doing)]));

        Assert.IsType<PlanEntry>(_timeline.Entries[^1]);
        Assert.Equal("1/2", plan.Progress);
    }

    [Fact]
    public void ReplaysARecordedReplyEndingWithItsOutcome()
    {
        _timeline.Add(new Prompt("Fix it", [new FileMention("src/A.cs"), new Picture("image/png", [1])], ["commit"]));
        _timeline.Add(new Reply([new Notice(NoticeKind.Context, "Summarized"), new Speech("Done.")], new Failed("Out of credit")));

        var prompt = Assert.IsType<PromptEntry>(_timeline.Entries[0]);
        Assert.Equal(["src/A.cs"], prompt.Files);
        Assert.Equal("1 picture", prompt.PicturesLabel);
        Assert.Equal(["commit"], prompt.Skills);
        Assert.IsType<NoticeEntry>(_timeline.Entries[1]);
        var outcome = Assert.IsType<OutcomeEntry>(_timeline.Entries[^1]);
        Assert.Equal(("Failed: Out of credit", true), (outcome.Text, outcome.WentWrong));
    }

    [Fact]
    public void IgnoresAResultOfNoActShown()
    {
        _timeline.Add(new ActResult("hidden", true, "plan saved"));

        Assert.Empty(_timeline.Entries);
    }
}
